namespace CommanderDeckBuilder.Core;

/// <summary>
/// Represents a Commander deck and owns its selected Commander information.
/// </summary>
public sealed class Deck
{
    private static readonly DeckValidationService Validation = new();
    private readonly List<DeckCardEntry> _entries = [];

    /// <summary>
    /// Gets whether tag modifications make the deck's working state dirty.
    /// </summary>
    public bool IsDirty { get; private set; }

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
    /// Gets the card entries currently stored in the deck.
    /// </summary>
    public IReadOnlyCollection<DeckCardEntry> Entries => _entries.AsReadOnly();

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
        Revalidate();
    }

    /// <summary>
    /// Replaces or removes the secondary Commander and revalidates every card entry.
    /// </summary>
    /// <param name="secondaryCommander">The replacement secondary Commander, or null to remove it.</param>
    public void ChangeSecondaryCommander(DeckCommander? secondaryCommander)
    {
        if (secondaryCommander is not null
            && secondaryCommander.Mechanic == DeckCommanderMechanic.Primary)
        {
            throw new ArgumentException(
                "A secondary Commander must use Partner, Background or Doctor's Companion.",
                nameof(secondaryCommander));
        }

        SecondaryCommander = secondaryCommander;
        EffectiveColorIdentity = CreateEffectiveColorIdentity(PrimaryCommander, secondaryCommander);
        Revalidate();
    }

    /// <summary>
    /// Adds a card to a zone, incrementing an existing entry for the same oracle and printing.
    /// </summary>
    public void AddCard(
        string oracleId,
        string selectedPrintingScryfallId,
        DeckZone zone,
        IEnumerable<string>? tags = null,
        OracleCard? oracleCard = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oracleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPrintingScryfallId);

        var existingEntry = _entries.SingleOrDefault(entry =>
            entry.OracleId.Equals(oracleId, StringComparison.Ordinal)
            && entry.SelectedPrintingScryfallId.Equals(selectedPrintingScryfallId, StringComparison.Ordinal));

        if (existingEntry is not null)
        {
            existingEntry.IncrementQuantity();
            if (oracleCard is not null)
            {
                existingEntry.AttachOracleCard(oracleCard);
            }

            if (existingEntry.AddTags(tags))
            {
                IsDirty = true;
            }
            Revalidate();
            return;
        }

        _entries.Add(new DeckCardEntry(
            oracleId,
            selectedPrintingScryfallId,
            1,
            zone,
            tags,
            oracleCard));
        Revalidate();
    }

    /// <summary>
    /// Increments the quantity of the entry for the selected printing.
    /// </summary>
    public void IncrementCard(string selectedPrintingScryfallId)
    {
        GetEntry(selectedPrintingScryfallId).IncrementQuantity();
        Revalidate();
    }

    /// <summary>
    /// Decrements the quantity of the entry for the selected printing.
    /// The entry is removed when its last copy is decremented.
    /// </summary>
    public void DecrementCard(string selectedPrintingScryfallId)
    {
        var entry = GetEntry(selectedPrintingScryfallId);
        if (!entry.DecrementQuantity())
        {
            _entries.Remove(entry);
        }
        Revalidate();
    }

    /// <summary>
    /// Removes every copy represented by the entry for the selected printing.
    /// </summary>
    public void RemoveCard(string selectedPrintingScryfallId)
    {
        _entries.Remove(GetEntry(selectedPrintingScryfallId));
        Revalidate();
    }

    /// <summary>
    /// Moves an entry to another deck zone without changing its card data.
    /// </summary>
    public void MoveCard(string selectedPrintingScryfallId, DeckZone destination)
    {
        GetEntry(selectedPrintingScryfallId).MoveTo(destination);
        Revalidate();
    }

    /// <summary>
    /// Changes the selected printing for an existing entry.
    /// </summary>
    public void ChangePrinting(
        string currentPrintingScryfallId,
        string selectedPrintingScryfallId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPrintingScryfallId);

        var source = GetEntry(currentPrintingScryfallId);
        var target = _entries.SingleOrDefault(entry =>
            !ReferenceEquals(entry, source)
            && entry.SelectedPrintingScryfallId.Equals(
                selectedPrintingScryfallId,
                StringComparison.Ordinal));

        if (target is not null)
        {
            if (!target.OracleId.Equals(source.OracleId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A printing identifier cannot be reassigned to a different Oracle card.");
            }

            if (target.MergeFrom(source))
            {
                IsDirty = true;
            }
            _entries.Remove(source);
            Revalidate();
            return;
        }

        source.ChangePrinting(selectedPrintingScryfallId);
        Revalidate();
    }

    /// <summary>
    /// Adds a custom tag to an existing entry.
    /// </summary>
    public void AddTag(string selectedPrintingScryfallId, string tag)
    {
        if (GetEntry(selectedPrintingScryfallId).AddTag(tag))
        {
            IsDirty = true;
        }
    }

    /// <summary>
    /// Removes a custom tag from an existing entry.
    /// </summary>
    public void RemoveTag(string selectedPrintingScryfallId, string tag)
    {
        if (GetEntry(selectedPrintingScryfallId).RemoveTag(tag))
        {
            IsDirty = true;
        }
    }

    /// <summary>
    /// Marks the current deck working state as clean after saving or committing.
    /// </summary>
    public void MarkAsClean()
    {
        IsDirty = false;
    }

    /// <summary>
    /// Restores the validity invariant: every entry's violation flags reflect the current deck state.
    /// Every mutator that can change validity calls this as its last step.
    /// </summary>
    private void Revalidate()
    {
        foreach (var result in Validation.Evaluate(this))
        {
            result.Entry.ApplyValidation(result.HasColorIdentityViolation, result.HasCopyLimitViolation);
        }
    }

    private DeckCardEntry GetEntry(string selectedPrintingScryfallId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPrintingScryfallId);

        return _entries.SingleOrDefault(entry =>
            entry.SelectedPrintingScryfallId.Equals(
                selectedPrintingScryfallId,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No deck card entry uses printing '{selectedPrintingScryfallId}'.");
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
