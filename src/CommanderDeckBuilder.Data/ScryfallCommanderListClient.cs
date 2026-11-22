using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommanderDeckBuilder.Data;

/// \brief Retrieves the oracle ids of every card Scryfall accepts as a commander (`is:commander`).
/// \details `is:commander` is a search-only predicate (it is not a field of the bulk data), so the
/// list is read from the paginated search endpoint during synchronization.
public sealed class ScryfallCommanderListClient
{
    private const string FirstPage = "https://api.scryfall.com/cards/search?q=is%3Acommander&unique=cards";
    /// \brief Pause between search pages: Scryfall allows 2 requests/second on /cards/search
    /// (scryfall.com/docs/api/rate-limits); 600 ms keeps a margin above the 500 ms minimum.
    public static readonly TimeSpan DefaultPageDelay = TimeSpan.FromMilliseconds(600);
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _pageDelay;

    /// \brief Initializes the client.
    /// \param httpClient The HTTP client used for the Scryfall requests.
    /// \param pageDelay The pause between page requests; defaults to \ref DefaultPageDelay.
    public ScryfallCommanderListClient(HttpClient httpClient, TimeSpan? pageDelay = null)
    {
        _httpClient = httpClient;
        _pageDelay = pageDelay ?? DefaultPageDelay;
        ScryfallHttp.EnsureUserAgent(_httpClient);
    }

    /// \brief Reads every result page and collects top-level and face oracle ids.
    /// \param cancellationToken Cancels the HTTP requests and the delays between them.
    /// \return The set of commander oracle ids.
    /// \exception HttpRequestException Thrown when a page request is unsuccessful.
    /// \exception InvalidDataException Thrown when a page cannot be parsed.
    public async Task<IReadOnlySet<string>> GetCommanderOracleIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var oracleIds = new HashSet<string>(StringComparer.Ordinal);
        Uri? next = new(FirstPage);
        while (next is not null)
        {
            var page = await ReadPageAsync(next, cancellationToken);
            foreach (var card in page.Data ?? [])
            {
                AddIfPresent(oracleIds, card.OracleId);
                foreach (var face in card.Faces ?? [])
                {
                    AddIfPresent(oracleIds, face.OracleId);
                }
            }

            next = page.HasMore && page.NextPage is not null ? new Uri(page.NextPage) : null;
            if (next is not null && _pageDelay > TimeSpan.Zero)
            {
                await Task.Delay(_pageDelay, cancellationToken);
            }
        }

        return oracleIds;
    }

    private async Task<SearchPage> ReadPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new ScryfallRateLimitedException(response.Headers.RetryAfter?.Delta, body);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Scryfall commander search failed with {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<SearchPage>(body)
            ?? throw new InvalidDataException("Scryfall commander search returned an empty page.");
    }

    private static void AddIfPresent(HashSet<string> oracleIds, string? oracleId)
    {
        if (!string.IsNullOrEmpty(oracleId))
        {
            oracleIds.Add(oracleId);
        }
    }

    /// \brief One page of Scryfall search results.
    private sealed class SearchPage
    {
        [JsonPropertyName("has_more")]
        public bool HasMore { get; init; }

        [JsonPropertyName("next_page")]
        public string? NextPage { get; init; }

        [JsonPropertyName("data")]
        public SearchCard[]? Data { get; init; }
    }

    /// \brief The fields of a search result card needed to identify it.
    private sealed class SearchCard
    {
        [JsonPropertyName("oracle_id")]
        public string? OracleId { get; init; }

        [JsonPropertyName("card_faces")]
        public SearchFace[]? Faces { get; init; }
    }

    /// \brief The identifying field of a search result card face.
    private sealed class SearchFace
    {
        [JsonPropertyName("oracle_id")]
        public string? OracleId { get; init; }
    }
}
