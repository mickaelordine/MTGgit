namespace CommanderDeckBuilder.Core;

/// <summary>
/// Searches the local oracle-card catalog.
/// </summary>
public interface ICardSearchService
{
    /// <summary>
    /// Searches oracle cards using commander, color, and result-limit filters.
    /// </summary>
    /// <param name="query">The user's card-name query.</param>
    /// <param name="commanderIdentity">The active commander's color identity.</param>
    /// <param name="enforceColorIdentity">Whether color filtering is enabled.</param>
    /// <param name="onlyCommanders">Whether only possible commanders are requested.</param>
    /// <param name="limit">The maximum number of results.</param>
    /// <returns>An awaitable sequence of ranked search results.</returns>
    Task<IReadOnlyList<CardSearchResult>> SearchCardsAsync(
        string query,
        ColorIdentity commanderIdentity,
        bool enforceColorIdentity = true,
        bool onlyCommanders = false,
        int limit = 50);
}
