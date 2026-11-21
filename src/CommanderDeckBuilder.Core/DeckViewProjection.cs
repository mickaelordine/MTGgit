namespace CommanderDeckBuilder.Core;

/// <summary>
/// Maps deck entries into category or tag groups for deck-list presentation.
/// </summary>
public sealed class DeckViewProjection
{
    private static readonly string[] CategoryPriority =
    [
        "Creature",
        "Planeswalker",
        "Battle",
        "Land",
        "Enchantment",
        "Artifact",
        "Instant",
        "Sorcery"
    ];

    /// <summary>
    /// Groups cards by category or tags according to tag mode.
    /// </summary>
    public IReadOnlyCollection<DeckViewGroup> GetGroups(Deck deck, bool tagsEnabled)
    {
        ArgumentNullException.ThrowIfNull(deck);

        if (!tagsEnabled)
        {
            return deck.Entries
                .GroupBy(GetCategory)
                .Select(group => new DeckViewGroup(group.Key, group.ToArray()))
                .ToArray();
        }

        return deck.Entries
            .SelectMany(GroupKeysFor)
            .GroupBy(pair => pair.Key)
            .Select(group => new DeckViewGroup(group.Key, group.Select(pair => pair.Entry).ToArray()))
            .ToArray();

        IEnumerable<(string Key, DeckCardEntry Entry)> GroupKeysFor(DeckCardEntry entry)
        {
            if (entry.Tags.Count == 0)
            {
                return [($"Untagged: {GetCategory(entry)}", entry)];
            }

            return entry.Tags.Select(tag => (tag, entry));
        }
    }

    /// <summary>
    /// Filters deck entries by a normalized tag.
    /// </summary>
    public IReadOnlyCollection<DeckCardEntry> FilterByTag(Deck deck, string? tag)
    {
        ArgumentNullException.ThrowIfNull(deck);

        var normalizedTag = TagText.Normalize(tag);
        if (normalizedTag.Length == 0)
        {
            return Array.Empty<DeckCardEntry>();
        }

        return deck.Entries
            .Where(entry => entry.Tags.Contains(normalizedTag, StringComparer.Ordinal))
            .ToArray();
    }

    /// <summary>
    /// Returns the primary category represented by a card type line.
    /// </summary>
    public string GetCategory(DeckCardEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        foreach (var category in CategoryPriority)
        {
            if (entry.CardTypeLine.Contains(category, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        return "Other";
    }
}

/// <summary>
/// Represents one deck-view group and its card entries.
/// </summary>
/// <param name="Name">The displayed group name.</param>
/// <param name="Entries">The entries contained in this group.</param>
public sealed record DeckViewGroup(
    string Name,
    IReadOnlyCollection<DeckCardEntry> Entries);
