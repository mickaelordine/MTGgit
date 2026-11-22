using System.Diagnostics.CodeAnalysis;

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
        IEnumerable<string>? tags = null,
        OracleCard? oracleCard = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oracleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPrintingScryfallId);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        OracleId = oracleId;
        SelectedPrintingScryfallId = selectedPrintingScryfallId;
        Quantity = quantity;
        Zone = zone;
        Tags = NormalizeTags(tags);
        AttachOracleCard(oracleCard ?? OracleCard.Placeholder(oracleId));
    }

    /// <summary>
    /// Replaces the entry's Oracle facts (name, type line, color identity) as one unit.
    /// </summary>
    /// <param name="oracleCard">The complete Oracle facts of the card.</param>
    [MemberNotNull(nameof(CardName), nameof(CardTypeLine))]
    internal void AttachOracleCard(OracleCard oracleCard)
    {
        ArgumentNullException.ThrowIfNull(oracleCard);

        CardName = oracleCard.Name;
        CardTypeLine = oracleCard.TypeLine;
        CardColorIdentity = oracleCard.ColorIdentity;
    }

    /// <summary>Gets the Scryfall oracle identifier.</summary>
    public string OracleId { get; }

    /// <summary>Gets the selected printing Scryfall identifier.</summary>
    public string SelectedPrintingScryfallId { get; private set; }

    /// <summary>Gets the positive number of copies represented by this entry.</summary>
    public int Quantity { get; private set; }

    /// <summary>Gets the deck zone containing this entry.</summary>
    public DeckZone Zone { get; private set; }

    /// <summary>Gets a snapshot of custom tags attached to this entry.</summary>
    public IReadOnlyCollection<string> Tags { get; private set; }

    /// <summary>Gets whether the entry is outside the effective Commander color identity.</summary>
    public bool HasColorIdentityViolation { get; private set; }

    /// <summary>Gets the card color identity used by deck validation (attached via <see cref="AttachOracleCard"/>; colorless placeholder otherwise).</summary>
    public ColorIdentity CardColorIdentity { get; private set; }

    /// <summary>Gets the oracle card name for copy-limit validation (placeholder: the oracle identifier).</summary>
    public string CardName { get; private set; }

    /// <summary>Gets the Scryfall type line for basic-land and copy-limit validation (placeholder: empty).</summary>
    public string CardTypeLine { get; private set; }

    /// <summary>Gets whether quantity violates Commander copy-limit rules.</summary>
    public bool HasCopyLimitViolation { get; private set; }

    /// <summary>Gets whether any deck validation rule is violated.</summary>
    public bool HasDeckValidationViolation =>
        HasColorIdentityViolation || HasCopyLimitViolation;

    internal void ApplyValidation(EntryValidation validation)
    {
        HasColorIdentityViolation = validation.HasColorIdentityViolation;
        HasCopyLimitViolation = validation.HasCopyLimitViolation;
    }

    /// <summary>
    /// Adds several custom tags to the entry.
    /// </summary>
    /// <param name="tags">The raw tags to add; may be null.</param>
    /// <returns><c>true</c> when at least one tag actually changed the tag set.</returns>
    internal bool AddTags(IEnumerable<string>? tags)
    {
        var tagsChanged = false;
        foreach (var tag in tags ?? [])
        {
            tagsChanged |= AddTag(tag);
        }

        return tagsChanged;
    }

    /// <summary>
    /// Merges quantities and tags when another entry represents the same Oracle and printing.
    /// </summary>
    internal bool MergeFrom(DeckCardEntry source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var tagsBefore = Tags.ToArray();
        Quantity += source.Quantity;
        Tags = NormalizeTags(source.Tags.Concat(Tags));
        return !tagsBefore.SequenceEqual(Tags, StringComparer.Ordinal);
    }

    /// <summary>
    /// Increments the number of copies represented by this entry.
    /// </summary>
    internal void IncrementQuantity()
    {
        Quantity++;
    }

    /// <summary>
    /// Decrements the number of copies; zero means the entry should be removed.
    /// </summary>
    /// <returns><see langword="true"/> when at least one copy remains.</returns>
    internal bool DecrementQuantity()
    {
        if (Quantity == 0)
        {
            throw new InvalidOperationException(
                "Cannot decrement an entry whose quantity is already zero.");
        }

        if (Quantity == 1)
        {
            Quantity = 0;
            return false;
        }

        Quantity--;
        return true;
    }

    /// <summary>
    /// Moves this entry to another deck zone.
    /// </summary>
    internal void MoveTo(DeckZone zone)
    {
        Zone = zone;
    }

    /// <summary>
    /// Changes the selected physical printing without changing card data or quantity.
    /// </summary>
    internal void ChangePrinting(string selectedPrintingScryfallId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPrintingScryfallId);
        SelectedPrintingScryfallId = selectedPrintingScryfallId;
    }

    /// <summary>
    /// Adds a custom tag to the entry's canonical tag set.
    /// A global tag replaces its local twin; a local tag is ignored while its global twin is present.
    /// </summary>
    /// <param name="tag">The raw tag text, starting with <c>#</c> or <c>#!</c>.</param>
    /// <returns><c>true</c> only when the tag set actually changed.</returns>
    internal bool AddTag(string tag)
    {
        var tagsBefore = Tags;
        Tags = NormalizeTags(Tags.Append(tag));
        return !tagsBefore.SequenceEqual(Tags, StringComparer.Ordinal);
    }

    /// <summary>
    /// Removes a custom tag if it is present.
    /// </summary>
    internal bool RemoveTag(string tag)
    {
        var normalizedTag = Tag.Parse(tag).Value;
        var originalCount = Tags.Count;
        Tags = Tags.Where(existingTag => !existingTag.Equals(normalizedTag, StringComparison.Ordinal))
            .OrderBy(existingTag => existingTag, StringComparer.Ordinal)
            .ToArray();
        return Tags.Count != originalCount;
    }

    private static IReadOnlyCollection<string> NormalizeTags(IEnumerable<string>? tags)
    {
        var parsed = (tags ?? []).Select(Tag.Parse).Distinct().ToArray();
        return parsed
            .Where(tag => !parsed.Any(other => other.Shadows(tag)))
            .Select(tag => tag.Value)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }
}
