using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CommanderDeckBuilder.Data;

public sealed record BulkDatasetMetadata(Uri DownloadUri, DateTimeOffset UpdatedAt);

public sealed class ScryfallBulkMetadataClient
{
    private const string DefaultCardsEndpoint = "https://api.scryfall.com/bulk-data/default-cards";
    private readonly HttpClient _httpClient;

    public ScryfallBulkMetadataClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BulkDatasetMetadata> GetDefaultCardsMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ScryfallBulkMetadataResponse>(
            DefaultCardsEndpoint,
            cancellationToken);

        if (response?.DownloadUri is null || response.UpdatedAt is null)
        {
            throw new InvalidDataException("Scryfall bulk metadata is missing required fields.");
        }

        if (!Uri.TryCreate(response.DownloadUri, UriKind.Absolute, out var downloadUri))
        {
            throw new InvalidDataException("Scryfall bulk metadata contains an invalid download URI.");
        }

        return new BulkDatasetMetadata(downloadUri, response.UpdatedAt.Value);
    }

    private sealed class ScryfallBulkMetadataResponse
    {
        [JsonPropertyName("download_uri")]
        public string? DownloadUri { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
