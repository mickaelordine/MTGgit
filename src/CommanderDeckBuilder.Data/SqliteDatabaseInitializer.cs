using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Data;

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
        """;

    private readonly LocalStoragePaths _paths;

    public SqliteDatabaseInitializer(LocalStoragePaths paths)
    {
        _paths = paths;
    }

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
