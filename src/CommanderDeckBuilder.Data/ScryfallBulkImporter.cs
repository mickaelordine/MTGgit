using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Data;

/// \brief Imports normalized Scryfall records into SQLite transactionally.
public sealed class ScryfallBulkImporter
{
    private const string DatasetName = "default-cards";
    private readonly LocalStoragePaths _paths;

    /// \brief Initializes an importer for the configured local database.
    /// \param paths The local storage paths containing the SQLite database.
    public ScryfallBulkImporter(LocalStoragePaths paths)
    {
        _paths = paths;
    }

    /// \brief Replaces the local card catalog with one Scryfall snapshot and updates the sync metadata,
    /// all in one transaction.
    /// \param cards The asynchronous sequence of normalized card records.
    /// \param commanderOracleIds The oracle ids Scryfall accepts as commanders (`is:commander`); they
    /// replace the previous list (an empty set leaves no commanders).
    /// \param updatedAt The remote dataset timestamp to store after success.
    /// \param cancellationToken Cancels enumeration, SQL operations, or commit.
    /// \return A task that completes after the transaction commits.
    /// \exception Exception Propagates import failures and causes transaction rollback (the previous
    /// catalog is kept).
    public async Task ImportAsync(
        IAsyncEnumerable<MappedScryfallCard> cards,
        IReadOnlySet<string> commanderOracleIds,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commanderOracleIds);

        await using var connection = new SqliteConnection(_paths.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction();
        await ClearCatalogAsync(connection, transaction, cancellationToken);

        await foreach (var mapped in cards.WithCancellation(cancellationToken))
        {
            await UpsertOracleCardAsync(connection, transaction, mapped.OracleCard, cancellationToken);
            await UpsertCardPrintingAsync(connection, transaction, mapped.CardPrinting, cancellationToken);
        }

        await InsertCommandersAsync(connection, transaction, commanderOracleIds, cancellationToken);
        await UpsertSyncMetadataAsync(connection, transaction, updatedAt, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// \brief Empties the card catalog so the import is a full snapshot (cards Scryfall removed
    /// disappear); the surrounding transaction restores everything if the import fails.
    /// \param connection The open SQLite connection.
    /// \param transaction The transaction that owns the write.
    /// \param cancellationToken Cancels the SQL command.
    private static async Task ClearCatalogAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM Commanders;
            DELETE FROM CardPrintings;
            DELETE FROM OracleCards;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// \brief Records which imported cards Scryfall accepts as commanders (`is:commander`); the
    /// catalog was emptied first, so the stored list is exactly this import's list.
    /// \param connection The open SQLite connection.
    /// \param transaction The transaction that owns the write.
    /// \param commanderOracleIds The commander oracle ids; ids missing from the catalog are ignored.
    /// \param cancellationToken Cancels the SQL commands.
    private static async Task InsertCommandersAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlySet<string> commanderOracleIds,
        CancellationToken cancellationToken)
    {
        foreach (var oracleId in commanderOracleIds)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR IGNORE INTO Commanders (OracleId)
                SELECT OracleId FROM OracleCards WHERE OracleId = $oracleId;
                """;
            command.Parameters.AddWithValue("$oracleId", oracleId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// \brief Inserts or updates one oracle card within the active transaction.
    /// \param connection The open SQLite connection.
    /// \param transaction The transaction that owns the write.
    /// \param card The normalized oracle card record.
    /// \param cancellationToken Cancels the SQL command.
    private static async Task UpsertOracleCardAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        OracleCardRecord card,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO OracleCards (
                OracleId, Name, NormalizedName, ManaCost, Cmc, TypeLine,
                OracleText, ColorIdentity, CommanderLegality)
            VALUES (
                $oracleId, $name, $normalizedName, $manaCost, $cmc, $typeLine,
                $oracleText, $colorIdentity, $commanderLegality)
            ON CONFLICT (OracleId) DO UPDATE SET
                Name = excluded.Name,
                NormalizedName = excluded.NormalizedName,
                ManaCost = excluded.ManaCost,
                Cmc = excluded.Cmc,
                TypeLine = excluded.TypeLine,
                OracleText = excluded.OracleText,
                ColorIdentity = excluded.ColorIdentity,
                CommanderLegality = excluded.CommanderLegality;
            """;
        AddParameter(command, "$oracleId", card.OracleId);
        AddParameter(command, "$name", card.Name);
        AddParameter(command, "$normalizedName", card.NormalizedName);
        AddParameter(command, "$manaCost", card.ManaCost);
        AddParameter(command, "$cmc", card.Cmc);
        AddParameter(command, "$typeLine", card.TypeLine);
        AddParameter(command, "$oracleText", card.OracleText);
        AddParameter(command, "$colorIdentity", card.ColorIdentity);
        AddParameter(command, "$commanderLegality", card.CommanderLegality);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// \brief Inserts or updates one card printing within the active transaction.
    /// \param connection The open SQLite connection.
    /// \param transaction The transaction that owns the write.
    /// \param printing The normalized printing record.
    /// \param cancellationToken Cancels the SQL command.
    private static async Task UpsertCardPrintingAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CardPrintingRecord printing,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO CardPrintings (
                ScryfallId, OracleId, SetCode, SetName, CollectorNumber, Rarity,
                ReleasedAt, ImageUriNormal, ImageUriArtCrop, BackImageUriNormal)
            VALUES (
                $scryfallId, $oracleId, $setCode, $setName, $collectorNumber, $rarity,
                $releasedAt, $imageUriNormal, $imageUriArtCrop, $backImageUriNormal)
            ON CONFLICT (ScryfallId) DO UPDATE SET
                OracleId = excluded.OracleId,
                SetCode = excluded.SetCode,
                SetName = excluded.SetName,
                CollectorNumber = excluded.CollectorNumber,
                Rarity = excluded.Rarity,
                ReleasedAt = excluded.ReleasedAt,
                ImageUriNormal = excluded.ImageUriNormal,
                ImageUriArtCrop = excluded.ImageUriArtCrop,
                BackImageUriNormal = excluded.BackImageUriNormal;
            """;
        AddParameter(command, "$scryfallId", printing.ScryfallId);
        AddParameter(command, "$oracleId", printing.OracleId);
        AddParameter(command, "$setCode", printing.SetCode);
        AddParameter(command, "$setName", printing.SetName);
        AddParameter(command, "$collectorNumber", printing.CollectorNumber);
        AddParameter(command, "$rarity", printing.Rarity);
        AddParameter(command, "$releasedAt", printing.ReleasedAt);
        AddParameter(command, "$imageUriNormal", printing.ImageUriNormal);
        AddParameter(command, "$imageUriArtCrop", printing.ImageUriArtCrop);
        AddParameter(command, "$backImageUriNormal", printing.BackImageUriNormal);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// \brief Stores the dataset timestamp within the active transaction.
    /// \param connection The open SQLite connection.
    /// \param transaction The transaction that owns the write.
    /// \param updatedAt The remote dataset timestamp.
    /// \param cancellationToken Cancels the SQL command.
    private static async Task UpsertSyncMetadataAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO SyncMetadata (DatasetName, UpdatedAt)
            VALUES ($datasetName, $updatedAt)
            ON CONFLICT (DatasetName) DO UPDATE SET UpdatedAt = excluded.UpdatedAt;
            """;
        AddParameter(command, "$datasetName", DatasetName);
        AddParameter(command, "$updatedAt", updatedAt.ToUniversalTime().ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// \brief Adds a nullable value as a SQLite command parameter.
    /// \param command The command receiving the parameter.
    /// \param name The parameter name.
    /// \param value The parameter value, or null.
    private static void AddParameter(
        SqliteCommand command,
        string name,
        object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }
}
