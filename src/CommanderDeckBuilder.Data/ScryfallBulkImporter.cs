using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Data;

public sealed class ScryfallBulkImporter
{
    private const string DatasetName = "default-cards";
    private readonly LocalStoragePaths _paths;

    public ScryfallBulkImporter(LocalStoragePaths paths)
    {
        _paths = paths;
    }

    public async Task ImportAsync(
        IAsyncEnumerable<MappedScryfallCard> cards,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_paths.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction();

        await foreach (var mapped in cards.WithCancellation(cancellationToken))
        {
            await UpsertOracleCardAsync(connection, transaction, mapped.OracleCard, cancellationToken);
            await UpsertCardPrintingAsync(connection, transaction, mapped.CardPrinting, cancellationToken);
        }

        await UpsertSyncMetadataAsync(connection, transaction, updatedAt, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

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
                OracleText, ColorIdentity, IsPossibleCommander, CommanderLegality)
            VALUES (
                $oracleId, $name, $normalizedName, $manaCost, $cmc, $typeLine,
                $oracleText, $colorIdentity, $isPossibleCommander, $commanderLegality)
            ON CONFLICT (OracleId) DO UPDATE SET
                Name = excluded.Name,
                NormalizedName = excluded.NormalizedName,
                ManaCost = excluded.ManaCost,
                Cmc = excluded.Cmc,
                TypeLine = excluded.TypeLine,
                OracleText = excluded.OracleText,
                ColorIdentity = excluded.ColorIdentity,
                IsPossibleCommander = excluded.IsPossibleCommander,
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
        AddParameter(command, "$isPossibleCommander", card.IsPossibleCommander);
        AddParameter(command, "$commanderLegality", card.CommanderLegality);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

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

    private static void AddParameter(
        SqliteCommand command,
        string name,
        object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }
}
