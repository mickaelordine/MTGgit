using System.Globalization;
using System.Text;

namespace CommanderDeckBuilder.Core;

/// <summary>
/// Builds the search key of a card name: what the user types is compared with what is stored
/// regardless of case and diacritics, while the card keeps its canonical Scryfall name.
/// </summary>
public static class CardNameNormalizer
{
    /// <summary>
    /// Trims, lower-cases and removes diacritics (<c>Nazgûl</c> → <c>nazgul</c>, <c>Æther</c> → <c>aether</c>).
    /// </summary>
    /// <param name="name">A card name or a search query.</param>
    /// <returns>The search key; empty for blank input.</returns>
    public static string ToSearchKey(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var decomposed = name.Trim().ToLowerInvariant()
            .Replace("æ", "ae", StringComparison.Ordinal)
            .Normalize(NormalizationForm.FormD);
        var key = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                key.Append(character);
            }
        }

        return key.ToString().Normalize(NormalizationForm.FormC);
    }
}
