namespace CommanderDeckBuilder.Core;

/// <summary>
/// Normalizes raw tag text to its canonical comparable form.
/// </summary>
internal static class TagText
{
    /// <summary>
    /// Trims, lowercases and collapses whitespace in a raw tag, returning empty for blank input.
    /// </summary>
    /// <param name="tag">The raw tag text, possibly null or whitespace.</param>
    /// <returns>The normalized tag, or an empty string when the input is blank.</returns>
    public static string Normalize(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        return string.Concat(
            tag.Trim().ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
