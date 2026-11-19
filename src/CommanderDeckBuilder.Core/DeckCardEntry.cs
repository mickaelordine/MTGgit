namespace CommanderDeckBuilder.Core;

/// <summary>
/// Represents one card or printing within a deck zone.
/// </summary>
public sealed class DeckCardEntry
{
    /// <summary>
    /// Creates a deck card entry and snapshots its quantity and tags.
    /// </summary>
    public DeckCardEntry(
        string oracleId,
        string selectedPrintingScryfallId,
        int quantity,
        DeckZone zone,
        IEnumerable<string>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oracleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPrintingScryfallId);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        OracleId = oracleId;
        SelectedPrintingScryfallId = selectedPrintingScryfallId;
        Quantity = quantity;
        Zone = zone;
        Tags = (tags ?? []).Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Gets the Scryfall oracle identifier.</summary>
    public string OracleId { get; }

    /// <summary>Gets the selected printing Scryfall identifier.</summary>
    public string SelectedPrintingScryfallId { get; }

    /// <summary>Gets the positive number of copies represented by this entry.</summary>
    public int Quantity { get; }

    /// <summary>Gets the deck zone containing this entry.</summary>
    public DeckZone Zone { get; }

    /// <summary>Gets a snapshot of custom tags attached to this entry.</summary>
    public IReadOnlyCollection<string> Tags { get; }

    /// <summary>Gets whether the entry is outside the effective Commander color identity.</summary>
    public bool HasColorIdentityViolation { get; }
}
