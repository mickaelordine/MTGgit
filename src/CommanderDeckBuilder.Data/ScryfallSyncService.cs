using Dapper;
using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Data;

public sealed record SyncProgress(long CardsProcessed, long? TotalBytes);

public sealed record SyncResult(bool Downloaded);

public sealed class ScryfallSyncService
{
    private readonly LocalStoragePaths _paths;
    private readonly ScryfallBulkMetadataClient _metadataClient;
    private readonly HttpClient _httpClient;
    private readonly ScryfallBulkImporter _importer;

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
                ScryfallBulkParser.ReadAsync(stream, cancellationToken),
                progress,
                totalBytes,
                cancellationToken),
            remote.UpdatedAt,
            cancellationToken);

        return new SyncResult(Downloaded: true);
    }

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
