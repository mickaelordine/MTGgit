namespace CommanderDeckBuilder.Core;

/// <summary>
/// Represents a Commander deck and owns its selected Commander information.
/// </summary>
public sealed class Deck
{
    private Deck(DeckCommander primaryCommander, DeckCommander? secondaryCommander)
    {
        PrimaryCommander = primaryCommander;
        SecondaryCommander = secondaryCommander;
        EffectiveColorIdentity = CreateEffectiveColorIdentity(primaryCommander, secondaryCommander);
    }

    /// <summary>
    /// Gets the required primary Commander selected for the deck.
    /// </summary>
    public DeckCommander PrimaryCommander { get; private set; }

    /// <summary>
    /// Gets the optional secondary Commander used by Partner, Background or Doctor's Companion mechanics.
    /// </summary>
    public DeckCommander? SecondaryCommander { get; private set; }

    /// <summary>
    /// Gets the combined color identity derived from all selected Commander slots.
    /// </summary>
    public ColorIdentity EffectiveColorIdentity { get; private set; }

    /// <summary>
    /// Creates a deck from the required primary Commander.
    /// </summary>
    /// <param name="primaryCommander">The primary Commander that defines the deck.</param>
    /// <param name="secondaryCommander">An optional valid secondary Commander slot.</param>
    /// <returns>A new deck with the selected primary Commander.</returns>
    public static Deck Create(
        DeckCommander? primaryCommander,
        DeckCommander? secondaryCommander = null)
    {
        ArgumentNullException.ThrowIfNull(primaryCommander);

        if (secondaryCommander is not null
            && secondaryCommander.Mechanic == DeckCommanderMechanic.Primary)
        {
            throw new ArgumentException(
                "A secondary Commander must use Partner, Background or Doctor's Companion.",
                nameof(secondaryCommander));
        }

        return new Deck(primaryCommander, secondaryCommander);
    }

    /// <summary>
    /// Replaces the primary Commander and derives the effective identity again.
    /// </summary>
    /// <param name="primaryCommander">The replacement primary Commander.</param>
    public void ChangePrimaryCommander(DeckCommander? primaryCommander)
    {
        ArgumentNullException.ThrowIfNull(primaryCommander);

        PrimaryCommander = primaryCommander;
        EffectiveColorIdentity = CreateEffectiveColorIdentity(primaryCommander, SecondaryCommander);
    }

    /// <summary>
    /// Replaces the secondary Commander and derives the effective identity again.
    /// </summary>
    /// <param name="secondaryCommander">The replacement secondary Commander.</param>
    public void ChangeSecondaryCommander(DeckCommander? secondaryCommander)
    {
        ArgumentNullException.ThrowIfNull(secondaryCommander);

        SecondaryCommander = secondaryCommander;
        EffectiveColorIdentity = CreateEffectiveColorIdentity(PrimaryCommander, secondaryCommander);
    }

    /// <summary>
    /// Determines whether Commander and Mainboard entries total exactly 100 cards.
    /// </summary>
    /// <param name="entries">The card entries to count.</param>
    /// <returns><see langword="true"/> when the complete deck contains exactly 100 cards.</returns>
    public static bool IsDeckSizeValid(IEnumerable<DeckCardEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var count = entries
            .Where(entry => entry.Zone is DeckZone.Commander or DeckZone.Mainboard)
            .Sum(entry => entry.Quantity);

        return count == 100;
    }

    private static ColorIdentity CreateEffectiveColorIdentity(
        DeckCommander primaryCommander,
        DeckCommander? secondaryCommander)
    {
        if (secondaryCommander is null)
        {
            return primaryCommander.Identity;
        }

        var combinedColors = primaryCommander.Identity
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Concat(secondaryCommander.Identity.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(value => value[0]);

        return ColorIdentity.From(combinedColors);
    }
}

/// <summary>
/// Identifies a Commander selected for a deck.
/// </summary>
/// <param name="Name">The display name of the selected Commander.</param>
/// <param name="Identity">The color identity derived from the selected Commander.</param>
/// <param name="Mechanic">The Commander pairing mechanic represented by this slot.</param>
public sealed record DeckCommander(
    string Name,
    ColorIdentity Identity,
    DeckCommanderMechanic Mechanic = DeckCommanderMechanic.Primary);
