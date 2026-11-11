using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommanderDeckBuilder.Data;

/// \brief Represents the remote metadata required to download a bulk dataset.
public sealed record BulkDatasetMetadata(Uri DownloadUri, DateTimeOffset UpdatedAt);

/// \brief Retrieves metadata for Scryfall bulk datasets.
public sealed class ScryfallBulkMetadataClient
{
    private const string DefaultCardsEndpoint = "https://api.scryfall.com/bulk-data/default-cards";
    private readonly HttpClient _httpClient;

    /// \brief Initializes a metadata client with an HTTP client.
    /// \param httpClient The HTTP client used for the Scryfall request.
    public ScryfallBulkMetadataClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MTGgit/1.0");
        }
    }

    /// \brief Retrieves and validates the default-cards dataset metadata.
    /// \param cancellationToken Cancels the HTTP request and JSON deserialization.
    /// \return The validated download URI and UTC update timestamp.
    /// \exception InvalidDataException Thrown when required metadata is missing or invalid.
    public async Task<BulkDatasetMetadata> GetDefaultCardsMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, DefaultCardsEndpoint);
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Scryfall metadata request failed with {(int)response.StatusCode}: {responseBody}");
        }

        var metadata = JsonSerializer.Deserialize<ScryfallBulkMetadataResponse>(responseBody);

        if (metadata?.EffectiveDownloadUri is null || metadata.UpdatedAt is null)
        {
            throw new InvalidDataException("Scryfall bulk metadata is missing required fields.");
        }

        if (!Uri.TryCreate(metadata.EffectiveDownloadUri, UriKind.Absolute, out var downloadUri))
        {
            throw new InvalidDataException("Scryfall bulk metadata contains an invalid download URI.");
        }

        return new BulkDatasetMetadata(downloadUri, metadata.UpdatedAt.Value);
    }

    /// \brief Represents the JSON response returned by Scryfall's bulk metadata endpoint.
    private sealed class ScryfallBulkMetadataResponse
    {
        [JsonPropertyName("download_uri")]
        public string? DownloadUri { get; init; }

        [JsonPropertyName("jsonl_download_uri")]
        public string? JsonlDownloadUri { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset? UpdatedAt { get; init; }

        public string? EffectiveDownloadUri => JsonlDownloadUri ?? DownloadUri;
    }
}
