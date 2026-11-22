namespace CommanderDeckBuilder.Core;

/// <summary>
/// Visibility scope of a custom card tag.
/// </summary>
public enum TagScope
{
    /// <summary>
    /// Deck-scoped tag, written with the <c>#</c> prefix.
    /// </summary>
    Local,

    /// <summary>
    /// Tag suggested in every deck, written with the <c>#!</c> prefix.
    /// </summary>
    Global
}

/// <summary>
/// A parsed, normalized custom card tag.
/// </summary>
public readonly record struct Tag
{
    private const string LocalPrefix = "#";
    private const string GlobalPrefix = "#!";

    private Tag(TagScope scope, string name)
    {
        Scope = scope;
        Name = name;
    }

    /// <summary>
    /// The tag's visibility scope.
    /// </summary>
    public TagScope Scope { get; }

    /// <summary>
    /// The normalized tag name, without prefix.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The canonical tag text, prefix included.
    /// </summary>
    public string Value => PrefixOf(Scope) + Name;

    /// <summary>
    /// Parses raw tag text into a normalized tag.
    /// </summary>
    /// <param name="raw">The raw tag text.</param>
    /// <returns>The parsed tag.</returns>
    public static Tag Parse(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);

        var text = Normalize(raw);
        var scope = text.StartsWith(GlobalPrefix, StringComparison.Ordinal)
            ? TagScope.Global
            : text.StartsWith(LocalPrefix, StringComparison.Ordinal)
                ? TagScope.Local
                : throw new ArgumentException(
                    "Custom tags must start with '#' or '#!'.",
                    nameof(raw));

        var name = text[PrefixOf(scope).Length..];
        if (name.Length == 0)
        {
            throw new ArgumentException("Custom tags must include a name.", nameof(raw));
        }

        return new Tag(scope, name);
    }

    private static string PrefixOf(TagScope scope) =>
        scope == TagScope.Global ? GlobalPrefix : LocalPrefix;

    /// <summary>
    /// Lowercases raw tag or query text and removes all whitespace, without validating it.
    /// </summary>
    /// <param name="text">The raw text, possibly null or whitespace.</param>
    /// <returns>The normalized text, or an empty string when the input is blank.</returns>
    internal static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return string.Concat(
            text.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
