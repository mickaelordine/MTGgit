namespace CommanderDeckBuilder.Core;

/// <summary>
/// Recalculates card color violations and deck-size validity for a Commander deck.
/// </summary>
public sealed class DeckValidationService
{
    private static readonly HashSet<string> UnlimitedCopyCards = new(StringComparer.OrdinalIgnoreCase)
    {
        "Templar Knight",
        "Tempest Hawk",
        "Hare Apparent",
        "Slime Against Humanity",
        "Nazgûl",
        "Nazgul",
        "Dragon's Approach",
        "Sphinx's Approach",
        "Shadowborn Apostle",
        "Persistent Petitioners",
        "Rat Colony",
        "Relentless Rats",
        "Cid, Timeless Artificer"
    };

    /// <summary>
    /// Updates every card entry with the active Commander color-identity result.
    /// </summary>
    /// <param name="deck">The deck to validate.</param>
    public void Validate(Deck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);

        foreach (var entry in deck.Entries)
        {
            entry.SetColorIdentityViolation(
                !entry.CardColorIdentity.IsSubsetOf(deck.EffectiveColorIdentity));
            entry.SetCopyLimitViolation(HasCopyLimitViolation(entry, deck));
        }
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

        // special-case cards that are allowed to have more than one copy in a deck but not infinite copies
        if (entry.CardName.Equals("Seven Dwarves", StringComparison.OrdinalIgnoreCase))
        {
            return combinedQuantity > 7;
        }
        
        // special-case cards that are allowed to have more than one copy in a deck but not infinite copies
        if (entry.CardName.Equals("Nazgûl", StringComparison.OrdinalIgnoreCase)
            || entry.CardName.Equals("Nazgul", StringComparison.OrdinalIgnoreCase))
        {
            return combinedQuantity > 9;
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
