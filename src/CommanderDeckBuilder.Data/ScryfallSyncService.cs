using Dapper;
using Microsoft.Data.Sqlite;
using System.IO.Compression;

namespace CommanderDeckBuilder.Data;

/// \brief Reports progress while cards are synchronized.
public sealed record SyncProgress(long CardsProcessed, long? TotalBytes);

/// \brief Represents the outcome of one synchronization attempt.
public sealed record SyncResult(bool Downloaded);

/// \brief Coordinates metadata lookup, download, parsing, and SQLite import.
public sealed class ScryfallSyncService
{
    private readonly LocalStoragePaths _paths;
    private readonly ScryfallBulkMetadataClient _metadataClient;
    private readonly HttpClient _httpClient;
    private readonly ScryfallBulkImporter _importer;

    /// \brief Initializes the synchronization service.
    /// \param paths The local storage paths containing the database.
    /// \param metadataClient The client used to retrieve remote dataset metadata.
    /// \param httpClient The HTTP client used to download the bulk JSON.
    public ScryfallSyncService(
        LocalStoragePaths paths,
        ScryfallBulkMetadataClient metadataClient,
        HttpClient httpClient)
    {
        _paths = paths;
        _metadataClient = metadataClient;
        _httpClient = httpClient;
        _importer = new ScryfallBulkImporter(paths);
    }

    /// \brief Synchronizes the local default-cards dataset when it is stale.
    /// \param progress Receives card and content-length progress updates, if supplied.
    /// \param cancellationToken Cancels metadata lookup, download, parsing, or import.
    /// \return A result indicating whether a download and import occurred.
    /// \exception HttpRequestException Thrown when the download response is unsuccessful.
    public async Task<SyncResult> SynchronizeAsync(
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var remote = await _metadataClient.GetDefaultCardsMetadataAsync(cancellationToken);
        var localUpdatedAt = await ReadLocalUpdatedAtAsync(cancellationToken);
        if (ScryfallFreshness.Decide(remote, localUpdatedAt) == BulkSyncDecision.UpToDate)
        {
            return new SyncResult(Downloaded: false);
        }

        using var response = await _httpClient.GetAsync(
            remote.DownloadUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var totalBytes = response.Content.Headers.ContentLength;
        await _importer.ImportAsync(
            TrackProgress(
                ReadCardsAsync(stream, remote.DownloadUri, cancellationToken),
                progress,
                totalBytes,
                cancellationToken),
            remote.UpdatedAt,
            cancellationToken);

        return new SyncResult(Downloaded: true);
    }

    /// \brief Selects the parser for the downloaded Scryfall representation.
    /// \param stream The downloaded response stream.
    /// \param downloadUri The URI used for the download.
    /// \param cancellationToken Cancels decompression and parsing.
    /// \return An asynchronous sequence of source card documents.
    private static async IAsyncEnumerable<ScryfallCardDocument> ReadCardsAsync(
        Stream stream,
        Uri downloadUri,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (downloadUri.AbsolutePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            await foreach (var card in ScryfallBulkParser.ReadJsonLinesAsync(
                               gzipStream,
                               cancellationToken))
            {
                yield return card;
            }

            yield break;
        }

        await foreach (var card in ScryfallBulkParser.ReadAsync(stream, cancellationToken))
        {
            yield return card;
        }
    }

    /// \brief Reads the locally committed default-cards timestamp.
    /// \param cancellationToken Cancels the database operation.
    /// \return The stored timestamp, or null when synchronization has never completed.
    /// \exception InvalidDataException Thrown when the stored timestamp is malformed.
    private async Task<DateTimeOffset?> ReadLocalUpdatedAtAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_paths.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        var value = await connection.ExecuteScalarAsync<string?>(
            new CommandDefinition(
                "SELECT UpdatedAt FROM SyncMetadata WHERE DatasetName = 'default-cards'",
                cancellationToken: cancellationToken));

        if (value is null)
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(value, out var updatedAt))
        {
            throw new InvalidDataException("Local Scryfall metadata contains an invalid timestamp.");
        }

        return updatedAt;
    }

    /// \brief Maps parsed cards while reporting the number processed.
    /// \param cards The asynchronous source-card sequence.
    /// \param progress The optional progress receiver.
    /// \param totalBytes The HTTP content length, when known.
    /// \param cancellationToken Cancels enumeration.
    /// \return An asynchronous sequence of mapped cards.
    private static async IAsyncEnumerable<MappedScryfallCard> TrackProgress(
        IAsyncEnumerable<ScryfallCardDocument> cards,
        IProgress<SyncProgress>? progress,
        long? totalBytes,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var count = 0L;
        await foreach (var card in cards.WithCancellation(cancellationToken))
        {
            count++;
            progress?.Report(new SyncProgress(count, totalBytes));
            yield return ScryfallCardMapper.Map(card);
        }
    }
}
