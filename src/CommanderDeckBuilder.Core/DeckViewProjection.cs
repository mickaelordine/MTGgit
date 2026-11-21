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
        throw new NotImplementedException();
    }

    /// <summary>
    /// Filters deck entries by a normalized tag.
    /// </summary>
    public IReadOnlyCollection<DeckCardEntry> FilterByTag(Deck deck, string? tag)
    {
        ArgumentNullException.ThrowIfNull(deck);
        throw new NotImplementedException();
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
