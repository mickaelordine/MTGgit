using System.Net;

namespace CommanderDeckBuilder.Data;

/// \brief Scryfall answered HTTP 429: the client exceeded the rate limit and is locked out for a while.
/// \details Never retry automatically: Scryfall warns that ignoring 429s leads to a network block.
public sealed class ScryfallRateLimitedException : HttpRequestException
{
    /// \brief Initializes the exception.
    /// \param retryAfter How long Scryfall asks to wait, when it says so.
    /// \param responseBody The error body returned by Scryfall.
    public ScryfallRateLimitedException(TimeSpan? retryAfter, string responseBody)
        : base(
            $"Scryfall rate limit exceeded (429); retry after {retryAfter?.TotalSeconds.ToString() ?? "an unspecified delay"} s: {responseBody}",
            inner: null,
            statusCode: HttpStatusCode.TooManyRequests)
    {
        RetryAfter = retryAfter;
    }

    /// \brief How long Scryfall asks to wait before the next request, if provided.
    public TimeSpan? RetryAfter { get; }
}

/// \brief Shared Scryfall HTTP etiquette (AGENTS.md §7): every client identifies itself.
internal static class ScryfallHttp
{
    /// \brief The User-Agent sent to Scryfall when the caller did not set one.
    public const string UserAgent = "MTGgit/1.0";

    /// \brief Adds the application User-Agent to a client that has none.
    /// \param httpClient The HTTP client used for Scryfall requests.
    public static void EnsureUserAgent(HttpClient httpClient)
    {
        if (httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }
    }
}
