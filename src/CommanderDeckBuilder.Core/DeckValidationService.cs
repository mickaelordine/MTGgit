namespace CommanderDeckBuilder.Core;

/// <summary>
/// Evaluates Commander color-identity, copy-limit and deck-size rules for a deck without changing it.
/// </summary>
public sealed class DeckValidationService
{
    private static readonly HashSet<string> UnlimitedCopyCards = new(StringComparer.OrdinalIgnoreCase)
    {
        "Templar Knight",
        "Tempest Hawk",
        "Hare Apparent",
        "Slime Against Humanity",
        "Dragon's Approach",
        "Sphinx's Approach",
        "Shadowborn Apostle",
        "Persistent Petitioners",
        "Rat Colony",
        "Relentless Rats",
        "Cid, Timeless Artificer"
    };

    private static readonly Dictionary<string, int> CappedCopyCards = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Seven Dwarves"] = 7,
        ["Nazgûl"] = 9
    };

    /// <summary>
    /// Computes the color-identity and copy-limit result of every entry without changing the deck.
    /// </summary>
    /// <param name="deck">The deck to evaluate.</param>
    /// <returns>One validation result per deck entry, in entry order.</returns>
    public IReadOnlyList<EntryValidation> Evaluate(Deck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);

        return deck.Entries
            .Select(entry => new EntryValidation(
                entry,
                !entry.CardColorIdentity.IsSubsetOf(deck.EffectiveColorIdentity),
                HasCopyLimitViolation(entry, deck)))
            .ToArray();
    }

    private static bool HasCopyLimitViolation(
        DeckCardEntry entry,
        Deck deck)
    {
        var combinedQuantity = deck.Entries
            .Where(candidate => candidate.OracleId.Equals(entry.OracleId, StringComparison.Ordinal))
            .Sum(candidate => candidate.Quantity);

        if (combinedQuantity <= 1)
        {
            return false;
        }

        var typeLineWords = entry.CardTypeLine.Split(
            [' ', '—', '-'],
            StringSplitOptions.RemoveEmptyEntries);
        if (typeLineWords.Contains("Basic", StringComparer.OrdinalIgnoreCase)
            && typeLineWords.Contains("Land", StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (CappedCopyCards.TryGetValue(entry.CardName, out var maximumCopies))
        {
            return combinedQuantity > maximumCopies;
        }

        return !UnlimitedCopyCards.Contains(entry.CardName);
    }

    /// <summary>
    /// Determines whether Commander and Mainboard contain exactly 100 cards.
    /// </summary>
    public bool IsDeckSizeValid(Deck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);
        return Deck.IsDeckSizeValid(deck.Entries);
    }
}

/// <summary>
/// The validation result computed for one deck entry.
/// </summary>
/// <param name="Entry">The evaluated entry.</param>
/// <param name="HasColorIdentityViolation">Whether the entry is outside the effective Commander color identity.</param>
/// <param name="HasCopyLimitViolation">Whether the entry's combined quantity breaks Commander copy-limit rules.</param>
public sealed record EntryValidation(
    DeckCardEntry Entry,
    bool HasColorIdentityViolation,
    bool HasCopyLimitViolation);
