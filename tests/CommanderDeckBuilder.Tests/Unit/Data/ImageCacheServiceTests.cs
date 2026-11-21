using System.IO;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Collections.Concurrent;
using CommanderDeckBuilder.Core;
using CommanderDeckBuilder.Data;
using CommanderDeckBuilder.UI;
using Microsoft.Extensions.DependencyInjection;

namespace CommanderDeckBuilder.Tests;

public sealed class ImageCacheServiceTests
{
    [Fact]
    public async Task Cache_hit_returns_existing_file_without_http_request()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));
        var paths = new LocalStoragePaths(root);
        Directory.CreateDirectory(paths.ImageCacheDirectory);
        var scryfallId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var localPath = Path.Combine(paths.ImageCacheDirectory, $"{scryfallId:D}.jpg");
        await File.WriteAllBytesAsync(localPath, [1, 2, 3]);
        var handler = new RejectHttpHandler();
        using var httpClient = new HttpClient(handler);
        var service = new ImageCacheService(paths, httpClient);

        var result = await service.GetImagePathAsync(
            scryfallId,
            new Uri("https://example.test/card.jpg"));

        Assert.Equal(localPath, result);
        Assert.Equal(0, handler.RequestCount);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task Cache_miss_downloads_and_saves_image()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));
        var paths = new LocalStoragePaths(root);
        var scryfallId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new StaticHttpHandler(
            new byte[] { 5, 6, 7 },
            HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var service = new ImageCacheService(paths, httpClient);

        var result = await service.GetImagePathAsync(
            scryfallId,
            new Uri("https://example.test/card.jpg"));

        Assert.Equal(
            Path.Combine(paths.ImageCacheDirectory, $"{scryfallId:D}.jpg"),
            result);
        Assert.Equal([5, 6, 7], await File.ReadAllBytesAsync(result));
        Assert.Equal(1, handler.RequestCount);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task Concurrent_cache_misses_download_once()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));
        var paths = new LocalStoragePaths(root);
        var scryfallId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new StaticHttpHandler(
            new byte[] { 9, 8, 7 },
            HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var service = new ImageCacheService(paths, httpClient);

        var first = service.GetImagePathAsync(
            scryfallId,
            new Uri("https://example.test/card.jpg"));
        var second = service.GetImagePathAsync(
            scryfallId,
            new Uri("https://example.test/card.jpg"));
        var pathsResult = await Task.WhenAll(first, second);

        Assert.Equal(pathsResult[0], pathsResult[1]);
        Assert.Equal(1, handler.RequestCount);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task Failed_download_removes_partial_file()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));
        var paths = new LocalStoragePaths(root);
        var scryfallId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new StaticHttpHandler([], HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler);
        var service = new ImageCacheService(paths, httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetImagePathAsync(
                scryfallId,
                new Uri("https://example.test/card.jpg")));

        Assert.False(File.Exists(
            Path.Combine(paths.ImageCacheDirectory, $"{scryfallId:D}.jpg")));
        Assert.Empty(
            Directory.Exists(paths.ImageCacheDirectory)
                ? Directory.GetFiles(paths.ImageCacheDirectory, "*.part")
                : []);
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Queued_downloads_respect_rate_limit()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));
        var paths = new LocalStoragePaths(root);
        var handler = new StaticHttpHandler([1], HttpStatusCode.OK);
        using var httpClient = new HttpClient(handler);
        var service = new ImageCacheService(paths, httpClient);
        var stopwatch = Stopwatch.StartNew();

        await service.GetImagePathAsync(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            new Uri("https://example.test/one.jpg"));
        await service.GetImagePathAsync(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            new Uri("https://example.test/two.jpg"));
        await service.GetImagePathAsync(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            new Uri("https://example.test/three.jpg"));
        stopwatch.Stop();

        Assert.True(
            stopwatch.Elapsed >= TimeSpan.FromMilliseconds(140),
            $"Three queued downloads completed in {stopwatch.Elapsed.TotalMilliseconds:F0} ms.");
        Assert.Equal(3, handler.RequestCount);
        var requestTimes = handler.RequestTimes.ToArray();
        Assert.Equal(3, requestTimes.Length);
        Assert.True(
            requestTimes[1] - requestTimes[0] >= TimeSpan.FromMilliseconds(60),
            $"The first request interval was {(requestTimes[1] - requestTimes[0]).TotalMilliseconds:F0} ms.");
        Assert.True(
            requestTimes[2] - requestTimes[1] >= TimeSpan.FromMilliseconds(60),
            $"The second request interval was {(requestTimes[2] - requestTimes[1]).TotalMilliseconds:F0} ms.");
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task Printing_switch_refreshes_artwork_path()
    {
        var cache = new FakeImageCacheService();
        var viewModel = new MainWindowViewModel(cache);
        var firstId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var secondId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        await viewModel.RefreshArtworkAsync(
            firstId,
            new Uri("https://example.test/one.jpg"));
        await viewModel.RefreshArtworkAsync(
            secondId,
            new Uri("https://example.test/two.jpg"));

        Assert.Equal(secondId, viewModel.SelectedPrintingScryfallId);
        Assert.Equal(
            new Uri("https://example.test/two.jpg"),
            viewModel.SelectedPrintingImageUri);
        Assert.Equal("cache-two.jpg", viewModel.SelectedPrintingImagePath);
    }

    [Fact]
    public void Image_cache_service_is_registered()
    {
        using var provider = AppHost.CreateServiceProvider();

        Assert.IsType<ImageCacheService>(
            provider.GetRequiredService<IImageCacheService>());
    }

    [Fact]
    public async Task Cache_and_http_failures_are_propagated()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));
        var paths = new LocalStoragePaths(root);
        var scryfallId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new StaticHttpHandler([], HttpStatusCode.BadGateway);
        using var httpClient = new HttpClient(handler);
        var service = new ImageCacheService(paths, httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetImagePathAsync(
                scryfallId,
                new Uri("https://example.test/failure.jpg")));

        Assert.Equal(1, handler.RequestCount);
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Older_artwork_refresh_cannot_overwrite_a_newer_selection()
    {
        var cache = new DelayedFirstImageCacheService();
        var viewModel = new MainWindowViewModel(cache);
        var firstId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var secondId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var olderRefresh = viewModel.RefreshArtworkAsync(
            firstId,
            new Uri("https://example.test/one.jpg"));
        await Task.Yield();
        await viewModel.RefreshArtworkAsync(
            secondId,
            new Uri("https://example.test/two.jpg"));
        cache.CompleteOlderRefresh();
        await olderRefresh;

        Assert.Equal(secondId, viewModel.SelectedPrintingScryfallId);
        Assert.Equal("cache-two.jpg", viewModel.SelectedPrintingImagePath);
    }

    private sealed class StaticHttpHandler(
        byte[] content,
        HttpStatusCode statusCode) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public ConcurrentQueue<DateTimeOffset> RequestTimes { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestTimes.Enqueue(DateTimeOffset.UtcNow);
            await Task.Yield();
            return new HttpResponseMessage(statusCode)
            {
                Content = new ByteArrayContent(content)
            };
        }
    }

    private sealed class FakeImageCacheService : IImageCacheService
    {
        public Task<string> GetImagePathAsync(
            Guid scryfallId,
            Uri imageUri,
            CancellationToken cancellationToken = default)
        {
            var result = scryfallId.ToString("D").StartsWith(
                "aaaaaaaa",
                StringComparison.Ordinal)
                ? "cache-one.jpg"
                : "cache-two.jpg";
            return Task.FromResult(result);
        }
    }

    private sealed class DelayedFirstImageCacheService : IImageCacheService
    {
        private readonly TaskCompletionSource<string> _olderCompletion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteOlderRefresh()
        {
            _olderCompletion.TrySetResult("cache-one.jpg");
        }

        public async Task<string> GetImagePathAsync(
            Guid scryfallId,
            Uri imageUri,
            CancellationToken cancellationToken = default)
        {
            if (scryfallId.ToString("D").StartsWith(
                "aaaaaaaa",
                StringComparison.Ordinal))
            {
                return await _olderCompletion.Task.WaitAsync(cancellationToken);
            }

            return "cache-two.jpg";
        }
    }

    private sealed class RejectHttpHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromException<HttpResponseMessage>(
                new InvalidOperationException("HTTP must not be called on a cache hit."));
        }
    }
}
