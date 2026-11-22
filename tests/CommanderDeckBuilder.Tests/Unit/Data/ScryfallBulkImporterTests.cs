using System.IO;
using System.Runtime.CompilerServices;
using CommanderDeckBuilder.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice4ScryfallBulkImporterTests
{
    private static readonly DateTimeOffset FirstTimestamp = DateTimeOffset.Parse("2024-01-02T03:04:05Z");
    private static readonly DateTimeOffset SecondTimestamp = DateTimeOffset.Parse("2024-01-03T03:04:05Z");

    [Fact]
    public async Task Persists_an_oracle_card_and_its_printing()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var importer = new ScryfallBulkImporter(fixture.Paths);
        var card = CardDocument("oracle-1", "Example Card", colorIdentity: ["G"]);

        await importer.ImportAsync(Cards(card), NoCommanders(), FirstTimestamp);

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
            CommanderList("oracle-esika"),
            FirstTimestamp);

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
            CommanderList("oracle-esika", "oracle-missing"),
            FirstTimestamp);

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(["oracle-esika"], connection.Query<string>("SELECT OracleId FROM Commanders"));
    }

    [Fact]
    public async Task Import_replaces_the_commander_list_of_a_previous_import()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var importer = new ScryfallBulkImporter(fixture.Paths);
        var catalog = new[] { CardDocument("oracle-a", "Card A"), CardDocument("oracle-b", "Card B") };

        await importer.ImportAsync(Cards(catalog), CommanderList("oracle-a"), FirstTimestamp);
        await importer.ImportAsync(Cards(catalog), CommanderList("oracle-b"), SecondTimestamp);

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(["oracle-b"], connection.Query<string>("SELECT OracleId FROM Commanders"));
    }

    [Fact]
    public async Task Rolls_back_cards_and_metadata_when_import_fails()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var importer = new ScryfallBulkImporter(fixture.Paths);
        var mapped = ScryfallCardMapper.Map(CardDocument("oracle-rollback", "Rollback Card"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => importer.ImportAsync(FailAfter(mapped), NoCommanders(), FirstTimestamp));

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
        var card = CardDocument("oracle-upsert", "Upsert Card", scryfallId: "card-upsert");

        await importer.ImportAsync(Cards(card), NoCommanders(), FirstTimestamp);
        await importer.ImportAsync(Cards(card), NoCommanders(), SecondTimestamp);

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM OracleCards WHERE OracleId = 'oracle-upsert'"));
        Assert.Equal(1, connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM CardPrintings WHERE ScryfallId = 'card-upsert'"));
        Assert.Equal(
            SecondTimestamp.ToUniversalTime().ToString("O"),
            connection.ExecuteScalar<string>(
                "SELECT UpdatedAt FROM SyncMetadata WHERE DatasetName = 'default-cards'"));
    }

    private static ScryfallCardDocument CardDocument(
        string oracleId,
        string name,
        string[]? colorIdentity = null,
        string? scryfallId = null) => new()
    {
        OracleId = oracleId,
        Name = name,
        TypeLine = "Creature",
        ColorIdentity = colorIdentity ?? [],
        ScryfallId = scryfallId ?? "card-" + oracleId,
        SetCode = "abc",
        SetName = "Example Set",
        CollectorNumber = "1",
        Rarity = "common"
    };

    /// <summary>A catalog in which Scryfall lists no commander.</summary>
    private static IReadOnlySet<string> NoCommanders() => new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlySet<string> CommanderList(params string[] oracleIds) =>
        new HashSet<string>(oracleIds, StringComparer.Ordinal);

    private static async IAsyncEnumerable<MappedScryfallCard> Cards(
        params ScryfallCardDocument[] documents)
    {
        await Task.Yield();
        foreach (var document in documents)
        {
            yield return ScryfallCardMapper.Map(document);
        }
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
