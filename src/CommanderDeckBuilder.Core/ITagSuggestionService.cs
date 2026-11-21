namespace CommanderDeckBuilder.Core;

/// <summary>
/// Provides normalized local and global tag autocomplete suggestions.
/// </summary>
public interface ITagSuggestionService
{
    /// <summary>
    /// Gets tag suggestions matching the supplied query.
    /// </summary>
    /// <param name="currentDeck">The deck whose local tags are included.</param>
    /// <param name="decks">The decks containing globally shared tags.</param>
    /// <param name="query">The normalized or unnormalized autocomplete query.</param>
    /// <returns>A unique, ordinally sorted collection of tag suggestions.</returns>
    IReadOnlyCollection<string> GetSuggestions(
        Deck currentDeck,
        IEnumerable<Deck>? decks,
        string? query);
}
