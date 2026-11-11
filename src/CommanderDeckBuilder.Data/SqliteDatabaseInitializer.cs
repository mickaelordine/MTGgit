using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Data;

/// \brief Creates the local SQLite database directories and schema.
public sealed class SqliteDatabaseInitializer
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Cards (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL,
            ColorIdentity TEXT NOT NULL,
            SetCode TEXT,
            CollectorNumber TEXT,
            ImageUri TEXT
        );

        CREATE TABLE IF NOT EXISTS Decks (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL,
            CommanderCardId TEXT,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS DeckCards (
            DeckId TEXT NOT NULL,
            CardId TEXT NOT NULL,
            Zone TEXT NOT NULL,
            Quantity INTEGER NOT NULL,
            Tags TEXT,
            PRIMARY KEY (DeckId, CardId, Zone)
        );

        CREATE TABLE IF NOT EXISTS Commits (
            Id TEXT NOT NULL PRIMARY KEY,
            DeckId TEXT NOT NULL,
            ParentId TEXT,
            Message TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            Snapshot TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS OracleCards (
            OracleId TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL,
            NormalizedName TEXT NOT NULL,
            ManaCost TEXT,
            Cmc REAL NOT NULL,
            TypeLine TEXT NOT NULL,
            OracleText TEXT,
            ColorIdentity TEXT NOT NULL,
            IsPossibleCommander INTEGER NOT NULL,
            CommanderLegality TEXT
        );

        CREATE TABLE IF NOT EXISTS CardPrintings (
            ScryfallId TEXT NOT NULL PRIMARY KEY,
            OracleId TEXT NOT NULL,
            SetCode TEXT NOT NULL,
            SetName TEXT NOT NULL,
            CollectorNumber TEXT NOT NULL,
            Rarity TEXT NOT NULL,
            ReleasedAt TEXT,
            ImageUriNormal TEXT,
            ImageUriArtCrop TEXT,
            BackImageUriNormal TEXT,
            FOREIGN KEY (OracleId) REFERENCES OracleCards (OracleId)
        );

        CREATE TABLE IF NOT EXISTS SyncMetadata (
            DatasetName TEXT NOT NULL PRIMARY KEY,
            UpdatedAt TEXT NOT NULL
        );
        """;

    private readonly LocalStoragePaths _paths;

    /// \brief Initializes the database initializer.
    /// \param paths The local storage paths used for database creation.
    public SqliteDatabaseInitializer(LocalStoragePaths paths)
    {
        _paths = paths;
    }

    /// \brief Creates required directories and all missing SQLite tables.
    /// \return This method completes after schema initialization.
    public void Initialize()
    {
        Directory.CreateDirectory(_paths.DatabaseDirectory);
        Directory.CreateDirectory(_paths.ImageCacheDirectory);

        using var connection = new SqliteConnection(_paths.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = Schema;
        command.ExecuteNonQuery();
    }
}
