using System.IO;
using CommanderDeckBuilder.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice1ScryfallSchemaTests
{
    [Fact]
    public void Initialization_creates_the_scryfall_bulk_tables()
    {
        using var fixture = new StorageFixture();

        fixture.Initializer.Initialize();

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        var tables = connection.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name").ToArray();

        Assert.Contains("OracleCards", tables);
        Assert.Contains("CardPrintings", tables);
        Assert.Contains("SyncMetadata", tables);
    }

    [Fact]
    public void Initialization_can_be_run_repeatedly_with_the_scryfall_schema()
    {
        using var fixture = new StorageFixture();

        fixture.Initializer.Initialize();
        fixture.Initializer.Initialize();

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        var tableCount = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('OracleCards', 'CardPrintings', 'SyncMetadata')");

        Assert.Equal(3, tableCount);
    }

    private sealed class StorageFixture : IDisposable
    {
        private readonly string _root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));

        public StorageFixture()
        {
            Paths = new LocalStoragePaths(_root);
            Initializer = new SqliteDatabaseInitializer(Paths);
        }

        public LocalStoragePaths Paths { get; }
        public SqliteDatabaseInitializer Initializer { get; }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
