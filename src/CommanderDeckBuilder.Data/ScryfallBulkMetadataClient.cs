using System.Net.Http.Json;
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
    }

    /// \brief Retrieves and validates the default-cards dataset metadata.
    /// \param cancellationToken Cancels the HTTP request and JSON deserialization.
    /// \return The validated download URI and UTC update timestamp.
    /// \exception InvalidDataException Thrown when required metadata is missing or invalid.
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

    /// \brief Represents the JSON response returned by Scryfall's bulk metadata endpoint.
    private sealed class ScryfallBulkMetadataResponse
    {
        [JsonPropertyName("download_uri")]
        public string? DownloadUri { get; init; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
