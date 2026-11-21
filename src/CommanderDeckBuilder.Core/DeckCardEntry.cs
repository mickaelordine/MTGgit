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
        string? cardName = null,
        string? cardTypeLine = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oracleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPrintingScryfallId);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        OracleId = oracleId;
        SelectedPrintingScryfallId = selectedPrintingScryfallId;
        Quantity = quantity;
        Zone = zone;
        Tags = NormalizeTags(tags);
        CardColorIdentity = ColorIdentity.Parse("C");
        CardName = string.IsNullOrWhiteSpace(cardName) ? oracleId : cardName;
        CardTypeLine = cardTypeLine ?? string.Empty;
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

    /// <summary>
    /// Gets or sets the card color identity used by deck validation.
    /// </summary>
    public ColorIdentity CardColorIdentity { get; set; }

    /// <summary>Gets or sets the oracle card name for copy-limit validation.</summary>
    public string CardName { get; set; }

    /// <summary>Gets or sets the Scryfall type line for basic-land and copy-limit validation.</summary>
    public string CardTypeLine { get; set; }

    /// <summary>Gets whether quantity violates Commander copy-limit rules.</summary>
    public bool HasCopyLimitViolation { get; private set; }

    /// <summary>Gets whether any deck validation rule is violated.</summary>
    public bool HasDeckValidationViolation =>
        HasColorIdentityViolation || HasCopyLimitViolation;

    internal void SetColorIdentityViolation(bool hasViolation)
    {
        HasColorIdentityViolation = hasViolation;
    }

    internal void SetCopyLimitViolation(bool hasViolation)
    {
        HasCopyLimitViolation = hasViolation;
    }

    /// <summary>
    /// Updates Oracle-level metadata and merges custom tags for an existing printing entry.
    /// </summary>
    public bool UpdateMetadata(
        string? cardName,
        string? cardTypeLine,
        IEnumerable<string>? tags)
    {
        var tagsChanged = false;

        if (!string.IsNullOrWhiteSpace(cardName))
        {
            CardName = cardName;
        }

        if (!string.IsNullOrWhiteSpace(cardTypeLine))
        {
            CardTypeLine = cardTypeLine;
        }

        foreach (var tag in tags ?? [])
        {
            tagsChanged |= AddTag(tag);
        }

        return tagsChanged;
    }

    /// <summary>
    /// Merges quantities and tags when another entry represents the same Oracle and printing.
    /// </summary>
    public bool MergeFrom(DeckCardEntry source)
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
    public void IncrementQuantity()
    {
        Quantity++;
    }

    /// <summary>
    /// Decrements the number of copies; zero means the entry should be removed.
    /// </summary>
    /// <returns><see langword="true"/> when at least one copy remains.</returns>
    public bool DecrementQuantity()
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
    public void MoveTo(DeckZone zone)
    {
        Zone = zone;
    }

    /// <summary>
    /// Changes the selected physical printing without changing card data or quantity.
    /// </summary>
    public void ChangePrinting(string selectedPrintingScryfallId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPrintingScryfallId);
        SelectedPrintingScryfallId = selectedPrintingScryfallId;
    }

    /// <summary>
    /// Adds a custom tag if it is not already present.
    /// </summary>
    public bool AddTag(string tag)
    {
        var normalizedTag = NormalizeTag(tag);
        var separatorIndex = normalizedTag.IndexOf('#');
        var tagName = normalizedTag[(separatorIndex + 1)..];

        if (normalizedTag.StartsWith("#!", StringComparison.Ordinal))
        {
            var localTag = "#" + tagName;
            var localRemoved = Tags.Contains(localTag, StringComparer.Ordinal);
            var globalExists = Tags.Contains(normalizedTag, StringComparer.Ordinal);

            Tags = Tags.Where(existingTag => !existingTag.Equals(localTag, StringComparison.Ordinal))
                .ToArray();

            if (!globalExists)
            {
                Tags = Tags.Append(normalizedTag).ToArray();
            }

            Tags = NormalizeTags(Tags);
            return localRemoved || !globalExists;
        }

        var globalTag = "#!" + tagName;
        if (Tags.Contains(globalTag, StringComparer.Ordinal))
        {
            return false;
        }

        if (!Tags.Contains(normalizedTag, StringComparer.Ordinal))
        {
            Tags = Tags.Append(normalizedTag)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(existingTag => existingTag, StringComparer.Ordinal)
                .ToArray();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a custom tag if it is present.
    /// </summary>
    public bool RemoveTag(string tag)
    {
        var normalizedTag = NormalizeTag(tag);
        var originalCount = Tags.Count;
        Tags = Tags.Where(existingTag => !existingTag.Equals(normalizedTag, StringComparison.Ordinal))
            .OrderBy(existingTag => existingTag, StringComparer.Ordinal)
            .ToArray();
        return Tags.Count != originalCount;
    }

    private static IReadOnlyCollection<string> NormalizeTags(IEnumerable<string>? tags)
    {
        var normalizedTags = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in tags ?? [])
        {
            normalizedTags.Add(NormalizeTag(tag));
        }

        foreach (var globalTag in normalizedTags
            .Where(tag => tag.StartsWith("#!", StringComparison.Ordinal))
            .ToArray())
        {
            normalizedTags.Remove("#" + globalTag[2..]);
        }

        return normalizedTags
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizeTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        var normalized = TagText.Normalize(tag);

        var prefix = normalized.StartsWith("#!", StringComparison.Ordinal)
            ? "#!"
            : normalized.StartsWith("#", StringComparison.Ordinal)
                ? "#"
                : throw new ArgumentException(
                    "Custom tags must start with '#' or '#!'.",
                    nameof(tag));

        var name = normalized[prefix.Length..];
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Custom tags must include a name.", nameof(tag));
        }

        return prefix + name;
    }
}
