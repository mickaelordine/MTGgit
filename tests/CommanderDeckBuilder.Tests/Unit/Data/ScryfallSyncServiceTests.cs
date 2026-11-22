using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using CommanderDeckBuilder.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice5ScryfallSyncServiceTests
{
    [Fact]
    public async Task Synchronizes_missing_data_and_reports_card_progress()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        using var httpClient = new HttpClient(new BulkStubHandler());
        var metadataClient = new ScryfallBulkMetadataClient(httpClient);
        var service = new ScryfallSyncService(fixture.Paths, metadataClient, httpClient);
        var progress = new List<SyncProgress>();

        var result = await service.SynchronizeAsync(
            new Progress<SyncProgress>(progress.Add));

        Assert.True(result.Downloaded);
        Assert.Contains(progress, item => item.CardsProcessed == 1);
        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM OracleCards"));
    }

    [Fact]
    public async Task Skips_download_when_local_dataset_is_current()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var handler = new BulkStubHandler();
        using var httpClient = new HttpClient(handler);
        var metadataClient = new ScryfallBulkMetadataClient(httpClient);
        var service = new ScryfallSyncService(fixture.Paths, metadataClient, httpClient);

        var first = await service.SynchronizeAsync();
        var second = await service.SynchronizeAsync();

        Assert.True(first.Downloaded);
        Assert.False(second.Downloaded);
        Assert.Equal(1, handler.DownloadRequests);
    }

    [Fact]
    public async Task Does_not_mark_the_dataset_current_when_download_fails()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        using var httpClient = new HttpClient(new FailingDownloadHandler());
        var metadataClient = new ScryfallBulkMetadataClient(httpClient);
        var service = new ScryfallSyncService(fixture.Paths, metadataClient, httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SynchronizeAsync());

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SyncMetadata"));
    }

    [Fact]
    public async Task Sync_does_not_import_non_deck_objects()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        using var httpClient = new HttpClient(new BulkStubHandler("""
            [
              {
                "oracle_id": "oracle-bolt",
                "name": "Lightning Bolt",
                "layout": "normal",
                "type_line": "Instant",
                "color_identity": ["R"],
                "id": "card-bolt",
                "set": "lea",
                "set_name": "Limited Edition Alpha",
                "collector_number": "161",
                "rarity": "common"
              },
              {
                "oracle_id": "oracle-treasure",
                "name": "Treasure",
                "layout": "token",
                "type_line": "Token Artifact — Treasure",
                "color_identity": [],
                "id": "card-treasure",
                "set": "tlci",
                "set_name": "The Lost Caverns of Ixalan Tokens",
                "collector_number": "21",
                "rarity": "common"
              }
            ]
            """));
        var metadataClient = new ScryfallBulkMetadataClient(httpClient);
        var service = new ScryfallSyncService(fixture.Paths, metadataClient, httpClient);

        var progress = new List<SyncProgress>();

        await service.SynchronizeAsync(new SynchronousProgress(progress.Add));

        Assert.Equal(2, progress.Max(item => item.CardsProcessed));
        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM OracleCards"));
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM CardPrintings"));
        Assert.Equal("Lightning Bolt", connection.ExecuteScalar<string>("SELECT Name FROM OracleCards"));
    }

    [Fact]
    public async Task Sync_stores_the_scryfall_commander_list()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        using var httpClient = new HttpClient(new BulkStubHandler(
            CommanderCandidatesJson,
            commanderOracleIds: ["oracle-background-commander"]));
        var metadataClient = new ScryfallBulkMetadataClient(httpClient);
        var service = new ScryfallSyncService(fixture.Paths, metadataClient, httpClient);

        await service.SynchronizeAsync();

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(["oracle-background-commander"], connection.Query<string>("SELECT OracleId FROM Commanders"));
    }

    [Fact]
    public async Task Sync_fails_without_downloading_when_the_commander_list_cannot_be_fetched()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();
        var handler = new BulkStubHandler(CommanderCandidatesJson, failCommanderSearch: true);
        using var httpClient = new HttpClient(handler);
        var metadataClient = new ScryfallBulkMetadataClient(httpClient);
        var service = new ScryfallSyncService(fixture.Paths, metadataClient, httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.SynchronizeAsync());

        Assert.Equal(0, handler.DownloadRequests);
        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM OracleCards"));
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM SyncMetadata"));
    }

    private const string CommanderCandidatesJson = """
        [
          {
            "oracle_id": "oracle-not-listed",
            "name": "Listed Nowhere",
            "layout": "normal",
            "type_line": "Legendary Creature — Human",
            "color_identity": ["W"],
            "id": "card-not-listed",
            "set": "abc",
            "set_name": "Example Set",
            "collector_number": "1",
            "rarity": "rare"
          },
          {
            "oracle_id": "oracle-background-commander",
            "name": "Listed Enchantment",
            "layout": "normal",
            "type_line": "Enchantment",
            "color_identity": ["G"],
            "id": "card-listed",
            "set": "abc",
            "set_name": "Example Set",
            "collector_number": "2",
            "rarity": "rare"
          }
        ]
        """;

    private sealed class SynchronousProgress(Action<SyncProgress> report) : IProgress<SyncProgress>
    {
        public void Report(SyncProgress value) => report(value);
    }

    private sealed class BulkStubHandler : HttpMessageHandler
    {
        private const string DefaultCardsJson = """
            [
              {
                "oracle_id": "oracle-sync",
                "name": "Sync Card",
                "type_line": "Creature",
                "color_identity": ["G"],
                "id": "card-sync",
                "set": "abc",
                "set_name": "Example Set",
                "collector_number": "1",
                "rarity": "common"
              }
            ]
            """;

        private readonly string _cardsJson;
        private readonly string[] _commanderOracleIds;
        private readonly bool _failCommanderSearch;

        public BulkStubHandler(
            string cardsJson = DefaultCardsJson,
            string[]? commanderOracleIds = null,
            bool failCommanderSearch = false)
        {
            _cardsJson = cardsJson;
            _commanderOracleIds = commanderOracleIds ?? [];
            _failCommanderSearch = failCommanderSearch;
        }

        public int DownloadRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath == "/cards/search")
            {
                if (_failCommanderSearch)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                }

                var data = string.Join(",", _commanderOracleIds.Select(id => $$"""{ "oracle_id": "{{id}}" }"""));
                return Task.FromResult(JsonResponse($$"""{ "has_more": false, "data": [ {{data}} ] }"""));
            }

            if (request.RequestUri?.AbsolutePath == "/bulk-data/default-cards")
            {
                return Task.FromResult(JsonResponse("""
                    {
                      "download_uri": "https://data.example.test/default-cards.json",
                      "updated_at": "2024-01-02T03:04:05.000Z"
                    }
                    """));
            }

            DownloadRequests++;
            return Task.FromResult(JsonResponse(_cardsJson));
        }

        private static HttpResponseMessage JsonResponse(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FailingDownloadHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath == "/bulk-data/default-cards")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {
                          "download_uri": "https://data.example.test/default-cards.json",
                          "updated_at": "2024-01-02T03:04:05.000Z"
                        }
                        """, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
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
