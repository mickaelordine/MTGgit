namespace CommanderDeckBuilder.Tests;

/// \brief Shared etiquette for integration tests that call the live Scryfall API.
/// \details Scryfall allows 10 requests/second in general and 2/second on /cards/search,
/// /cards/named, /cards/random and /cards/collection (scryfall.com/docs/api/rate-limits); a violation
/// returns 429 and a 30-second lockout. Live tests run in one non-parallel collection and call
/// \ref ThrottleAsync before every request, so requests are at least 600 ms apart.
public static class ScryfallLiveApi
{
    /// \brief Name of the xUnit collection that serializes all live-API test classes.
    public const string CollectionName = "Scryfall live API";

    private static readonly TimeSpan MinimumGap = TimeSpan.FromMilliseconds(600);
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTimeOffset _lastRequest = DateTimeOffset.MinValue;

    /// \brief Waits until at least 600 ms have passed since the previous live request.
    /// \return A task that completes when the next request may be sent.
    public static async Task ThrottleAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var wait = _lastRequest + MinimumGap - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait);
            }

            _lastRequest = DateTimeOffset.UtcNow;
        }
        finally
        {
            Gate.Release();
        }
    }
}

/// \brief Serializes every test class that talks to the live Scryfall API.
[CollectionDefinition(ScryfallLiveApi.CollectionName, DisableParallelization = true)]
public sealed class ScryfallLiveApiCollection;
