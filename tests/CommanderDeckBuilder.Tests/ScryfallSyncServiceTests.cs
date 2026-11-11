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

    private sealed class BulkStubHandler : HttpMessageHandler
    {
        public int DownloadRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
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
            return Task.FromResult(JsonResponse("""
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
                """));
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
