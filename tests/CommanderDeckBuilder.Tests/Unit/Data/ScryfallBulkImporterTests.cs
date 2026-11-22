using System.IO;
using System.Runtime.CompilerServices;
using CommanderDeckBuilder.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice4ScryfallBulkImporterTests
{
    [Fact]
    public async Task Persists_an_oracle_card_and_its_printing()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var importer = new ScryfallBulkImporter(fixture.Paths);
        var mapped = ScryfallCardMapper.Map(new ScryfallCardDocument
        {
            OracleId = "oracle-1",
            Name = "Example Card",
            TypeLine = "Creature",
            ColorIdentity = ["G"],
            ScryfallId = "card-1",
            SetCode = "abc",
            SetName = "Example Set",
            CollectorNumber = "1",
            Rarity = "common"
        });

        await importer.ImportAsync(
            One(mapped),
            DateTimeOffset.Parse("2024-01-02T03:04:05Z"));

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM OracleCards"));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM CardPrintings"));
        Assert.Equal("G", connection.ExecuteScalar<string>(
            "SELECT ColorIdentity FROM OracleCards WHERE OracleId = 'oracle-1'"));
    }

    [Fact]
    public async Task Import_writes_commander_oracle_ids_to_the_commanders_table()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var importer = new ScryfallBulkImporter(fixture.Paths);

        await importer.ImportAsync(
            Cards(CardDocument("oracle-esika", "Esika, God of the Tree"), CardDocument("oracle-bolt", "Lightning Bolt")),
            DateTimeOffset.Parse("2024-01-02T03:04:05Z"),
            commanderOracleIds: new HashSet<string>(StringComparer.Ordinal) { "oracle-esika" });

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(["oracle-esika"], connection.Query<string>("SELECT OracleId FROM Commanders"));
    }

    [Fact]
    public async Task Import_ignores_commander_ids_not_in_the_catalog()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var importer = new ScryfallBulkImporter(fixture.Paths);

        await importer.ImportAsync(
            Cards(CardDocument("oracle-esika", "Esika, God of the Tree")),
            DateTimeOffset.Parse("2024-01-02T03:04:05Z"),
            commanderOracleIds: new HashSet<string>(StringComparer.Ordinal) { "oracle-esika", "oracle-missing" });

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(["oracle-esika"], connection.Query<string>("SELECT OracleId FROM Commanders"));
    }

    private static ScryfallCardDocument CardDocument(string oracleId, string name) => new()
    {
        OracleId = oracleId,
        Name = name,
        TypeLine = "Creature",
        ScryfallId = "card-" + oracleId,
        SetCode = "abc",
        SetName = "Example Set",
        CollectorNumber = "1",
        Rarity = "common"
    };

    private static async IAsyncEnumerable<MappedScryfallCard> Cards(params ScryfallCardDocument[] documents)
    {
        foreach (var document in documents)
        {
            yield return ScryfallCardMapper.Map(document);
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Rolls_back_cards_and_metadata_when_import_fails()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var importer = new ScryfallBulkImporter(fixture.Paths);
        var mapped = ScryfallCardMapper.Map(new ScryfallCardDocument
        {
            OracleId = "oracle-rollback",
            Name = "Rollback Card",
            TypeLine = "Creature",
            ScryfallId = "card-rollback",
            SetCode = "abc",
            SetName = "Example Set",
            CollectorNumber = "1",
            Rarity = "common"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => importer.ImportAsync(
                FailAfter(mapped),
                DateTimeOffset.Parse("2024-01-02T03:04:05Z")));

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM OracleCards"));
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM CardPrintings"));
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SyncMetadata"));
    }

    [Fact]
    public async Task Reimporting_the_same_dataset_upserts_rows_and_metadata()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var importer = new ScryfallBulkImporter(fixture.Paths);
        var mapped = ScryfallCardMapper.Map(new ScryfallCardDocument
        {
            OracleId = "oracle-upsert",
            Name = "Upsert Card",
            TypeLine = "Creature",
            ScryfallId = "card-upsert",
            SetCode = "abc",
            SetName = "Example Set",
            CollectorNumber = "1",
            Rarity = "common"
        });
        
        var firstTimestamp = DateTimeOffset.Parse("2024-01-02T03:04:05Z");
        var secondTimestamp = DateTimeOffset.Parse("2024-01-03T03:04:05Z");

        await importer.ImportAsync(One(mapped), firstTimestamp);
        await importer.ImportAsync(One(mapped), secondTimestamp);

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM OracleCards WHERE OracleId = 'oracle-upsert'"));
        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM CardPrintings WHERE ScryfallId = 'card-upsert'"));
        Assert.Equal(
            secondTimestamp.ToUniversalTime().ToString("O"),
            connection.ExecuteScalar<string>(
                "SELECT UpdatedAt FROM SyncMetadata WHERE DatasetName = 'default-cards'"));
    }

    private static async IAsyncEnumerable<MappedScryfallCard> One(
        MappedScryfallCard mapped,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return mapped;
    }

    private static async IAsyncEnumerable<MappedScryfallCard> FailAfter(
        MappedScryfallCard mapped,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return mapped;
        throw new InvalidOperationException("Import fixture failure.");
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
