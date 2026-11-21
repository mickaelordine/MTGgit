using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Data;

/// <summary>
/// Caches Scryfall card artwork under the configured local image directory.
/// </summary>
public sealed class ImageCacheService : IImageCacheService
{
    private static readonly TimeSpan RequestSpacing = TimeSpan.FromMilliseconds(75);
    private readonly object _downloadLock = new();
    private readonly Dictionary<Guid, Lazy<Task<string>>> _inFlightDownloads = [];
    private readonly LocalStoragePaths _paths;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _downloadGate = new(1, 1);
    private readonly RequestThrottle _requestThrottle = new(RequestSpacing);

    /// <summary>
    /// Creates an image cache service.
    /// </summary>
    /// <param name="paths">The local storage paths containing the image cache directory.</param>
    /// <param name="httpClient">The HTTP client used for CDN downloads.</param>
    public ImageCacheService(LocalStoragePaths paths, HttpClient httpClient)
    {
        _paths = paths;
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<string> GetImagePathAsync(
        Guid scryfallId,
        Uri imageUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageUri);

        Directory.CreateDirectory(_paths.ImageCacheDirectory);
        var localPath = Path.Combine(
            _paths.ImageCacheDirectory,
            $"{scryfallId:D}.jpg");

        if (File.Exists(localPath))
        {
            return localPath;
        }

        Lazy<Task<string>> download;
        lock (_downloadLock)
        {
            if (!_inFlightDownloads.TryGetValue(scryfallId, out var existing))
            {
                existing = new Lazy<Task<string>>(
                    () => DownloadAsync(scryfallId, imageUri, localPath, cancellationToken),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                _inFlightDownloads.Add(scryfallId, existing);
            }

            download = existing;
        }

        try
        {
            return await download.Value.ConfigureAwait(false);
        }
        finally
        {
            lock (_downloadLock)
            {
                if (_inFlightDownloads.TryGetValue(scryfallId, out var existing)
                    && ReferenceEquals(existing, download))
                {
                    _inFlightDownloads.Remove(scryfallId);
                }
            }
        }
    }

    private async Task<string> DownloadAsync(
        Guid scryfallId,
        Uri imageUri,
        string localPath,
        CancellationToken cancellationToken)
    {
        await _downloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var temporaryPath = $"{localPath}.{Guid.NewGuid():N}.part";

        try
        {
            if (File.Exists(localPath))
            {
                return localPath;
            }

            await _requestThrottle.WaitAsync(cancellationToken).ConfigureAwait(false);
            using var response = await _httpClient.GetAsync(
                    imageUri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            await destination.DisposeAsync().ConfigureAwait(false);
            File.Move(temporaryPath, localPath);
            return localPath;
        }
        finally
            {
                // is something went wrong if we try to delete, catch up exceptions and ignore them, we don't want to crash the app because of a failed cleanup, then release the gate for the next download
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException) { /* best effort: il file orfano non è critico */ }
                catch (UnauthorizedAccessException) { }
                finally
                {
                    _downloadGate.Release();
                }
            }
    }

    private sealed class RequestThrottle(TimeSpan spacing)
    {
        private readonly object _sync = new();
        private DateTimeOffset _lastRequestStarted = DateTimeOffset.MinValue;

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            TimeSpan elapsed;
            TimeSpan wait;

            lock (_sync)
            {
                elapsed = DateTimeOffset.UtcNow - _lastRequestStarted;
                wait = elapsed < spacing ? spacing - elapsed : TimeSpan.Zero;
            }

            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            }

            lock (_sync)
            {
                _lastRequestStarted = DateTimeOffset.UtcNow;
            }
        }
    }
}
