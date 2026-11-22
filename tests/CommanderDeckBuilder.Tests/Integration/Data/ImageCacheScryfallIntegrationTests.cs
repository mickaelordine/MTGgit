using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.IO;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

/// <summary>
/// Verifies the on-disk image cache against live Scryfall named-card data.
/// </summary>
[Collection(ScryfallLiveApi.CollectionName)]
public sealed class ImageCacheScryfallIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Sol_ring_artwork_is_downloaded_and_cached_from_live_scryfall()
    {
        using var httpClient = CreateHttpClient();
        var card = await GetNamedCardAsync(httpClient, "Sol Ring");
        var scryfallId = Guid.Parse(card.Id);
        var imageUri = new Uri(card.ImageUris!.Normal);
        var paths = new LocalStoragePaths(
            Path.Combine(
                Path.GetTempPath(),
                "CommanderDeckBuilderTests",
                Guid.NewGuid().ToString("N")));
        try
        {
            var cache = new ImageCacheService(paths, httpClient);

            var localPath = await cache.GetImagePathAsync(scryfallId, imageUri);

            Assert.Equal(
                Path.Combine(paths.ImageCacheDirectory, $"{scryfallId:D}.jpg"),
                localPath);
            Assert.True(File.Exists(localPath));
            Assert.True(new FileInfo(localPath).Length > 0);
        }
        finally
        {
            if (Directory.Exists(paths.RootDirectory))
            {
                Directory.Delete(paths.RootDirectory, recursive: true);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task A_second_live_scryfall_cache_lookup_never_calls_http()
    {
        using var httpClient = CreateHttpClient();
        var card = await GetNamedCardAsync(httpClient, "Sol Ring");
        var scryfallId = Guid.Parse(card.Id);
        var imageUri = new Uri(card.ImageUris!.Normal);
        var paths = new LocalStoragePaths(
            Path.Combine(
                Path.GetTempPath(),
                "CommanderDeckBuilderTests",
                Guid.NewGuid().ToString("N")));
        try
        {
            var cache = new ImageCacheService(paths, httpClient);

            var firstPath = await cache.GetImagePathAsync(scryfallId, imageUri);
            using var rejectingClient = new HttpClient(new RejectHttpHandler());
            var diskCache = new ImageCacheService(paths, rejectingClient);

            var secondPath = await diskCache.GetImagePathAsync(scryfallId, imageUri);

            Assert.Equal(firstPath, secondPath);
        }
        finally
        {
            if (Directory.Exists(paths.RootDirectory))
            {
                Directory.Delete(paths.RootDirectory, recursive: true);
            }
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MTGgit/1.0");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return httpClient;
    }

    private static async Task<NamedCard> GetNamedCardAsync(
        HttpClient httpClient,
        string name)
    {
        var endpoint =
            $"https://api.scryfall.com/cards/named?exact={Uri.EscapeDataString(name)}";
        await ScryfallLiveApi.ThrottleAsync();
        var card = await httpClient.GetFromJsonAsync<NamedCard>(endpoint);
        if (card is null)
        {
            throw new InvalidDataException(
                $"Scryfall returned no card for '{name}'.");
        }

        if (string.IsNullOrWhiteSpace(card.Id)
            || card.ImageUris?.Normal is null)
        {
            throw new InvalidDataException(
                $"Scryfall returned incomplete image data for '{name}'.");
        }

        return card;
    }

    private sealed class NamedCard
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("image_uris")]
        public LiveImageUris? ImageUris { get; init; }
    }

    private sealed class LiveImageUris
    {
        [JsonPropertyName("normal")]
        public string Normal { get; init; } = string.Empty;
    }

    private sealed class RejectHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(
                new InvalidOperationException(
                    "The second cache lookup must not call HTTP."));
        }
    }
}
