namespace CommanderDeckBuilder.Data;

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
