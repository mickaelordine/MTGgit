namespace CommanderDeckBuilder.Core;

/// <summary>
/// Applies the non-UI search eligibility rules.
/// </summary>
public static class CardSearchEligibility
{
    /// <summary>
    /// Determines whether a card passes commander and color-identity filters.
    /// </summary>
    /// <param name="card">The oracle-card candidate to evaluate.</param>
    /// <param name="commanderIdentity">The active commander's color identity.</param>
    /// <param name="enforceColorIdentity">Whether the subset rule is enabled.</param>
    /// <param name="onlyCommanders">Whether non-commanders must be excluded.</param>
    /// <returns><see langword="true"/> when the candidate is eligible.</returns>
    public static bool IsEligible(
        CardSearchCandidate card,
        ColorIdentity commanderIdentity,
        bool enforceColorIdentity,
        bool onlyCommanders)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (onlyCommanders && !card.IsPossibleCommander)
        {
            return false;
        }

        return !enforceColorIdentity
            || ColorIdentity.Parse(card.ColorIdentity).IsSubsetOf(commanderIdentity);
    }
}
