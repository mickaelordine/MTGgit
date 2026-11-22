namespace CommanderDeckBuilder.Core;

/// <summary>
/// Aggregates local and global tag autocomplete suggestions.
/// </summary>
public sealed class TagSuggestionService : ITagSuggestionService
{
    /// <inheritdoc />
    public IReadOnlyCollection<string> GetSuggestions(
        Deck currentDeck,
        IEnumerable<Deck>? decks,
        string? query)
    {
        ArgumentNullException.ThrowIfNull(currentDeck);
        var normalizedQuery = Tag.Normalize(query);
        if (normalizedQuery.Length == 0)
        {
            return Array.Empty<string>();
        }

        var suggestions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in currentDeck.Entries)
        {
            foreach (var tag in entry.Tags)
            {
                if (Tag.Parse(tag).Scope == TagScope.Local)
                {
                    suggestions.Add(tag);
                }
            }
        }

        foreach (var deck in decks ?? [])
        {
            ArgumentNullException.ThrowIfNull(deck);
            foreach (var entry in deck.Entries)
            {
                foreach (var tag in entry.Tags)
                {
                    if (Tag.Parse(tag).Scope == TagScope.Global)
                    {
                        suggestions.Add(tag);
                    }
                }
            }
        }

        return suggestions
            .Where(tag => tag.Contains(normalizedQuery, StringComparison.Ordinal))
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToArray();
    }
}
