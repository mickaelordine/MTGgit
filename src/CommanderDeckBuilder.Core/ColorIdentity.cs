namespace CommanderDeckBuilder.Core;

/// <summary>
/// Immutable set of canonical Magic color-identity symbols.
/// </summary>
public readonly record struct ColorIdentity
{
    private static readonly char[] AllowedColors = ['B', 'G', 'R', 'U', 'W']; // C (colorless) is allowed but not included in this list because it is a special case.
    private readonly string _canonicalValue;

    private ColorIdentity(string canonicalValue)
    {
        _canonicalValue = canonicalValue;
    }

    /// <summary>
    /// Parses a comma-separated color identity such as <c>U,B</c>.
    /// </summary>
    /// <param name="value">The persisted or user-provided color identity.</param>
    /// <returns>The canonical immutable color identity.</returns>
    public static ColorIdentity Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return From(value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Length == 1
                ? char.ToUpperInvariant(token[0])
                : throw new FormatException($"Invalid color identity token '{token}'.")));
    }

    /// <summary>
    /// Creates a canonical identity from color symbols and snapshots the input sequence.
    /// </summary>
    /// <param name="colors">The color symbols that form the identity.</param>
    /// <returns>The canonical immutable color identity.</returns>
    public static ColorIdentity From(IEnumerable<char> colors)
    {
        ArgumentNullException.ThrowIfNull(colors);

        var distinctColors = colors
            .Select(char.ToUpperInvariant)
            .Distinct()
            .ToArray();

        if (distinctColors.Any(color => color != 'C' && !AllowedColors.Contains(color)))
        {
            throw new FormatException("Invalid color identity.");
        }

        if (distinctColors.Contains('C') && distinctColors.Length > 1)
        {
            throw new FormatException("Colorless identity cannot contain other colors.");
        }

        var canonicalValue = new string(
            distinctColors.Contains('C')
                ? ['C']
                : AllowedColors.Where(distinctColors.Contains).ToArray());
        return new ColorIdentity(canonicalValue);
    }

    /// <summary>
    /// Determines whether this identity contains the specified color symbol.
    /// </summary>
    /// <param name="color">The color symbol to find.</param>
    /// <returns><see langword="true"/> when the symbol is present; otherwise, <see langword="false"/>.</returns>
    public bool Contains(char color)
    {
        return _canonicalValue.Contains(char.ToUpperInvariant(color), StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether this card identity is allowed by a commander's identity.
    /// </summary>
    /// <param name="commanderIdentity">The commander's color identity.</param>
    /// <returns><see langword="true"/> when every card color is allowed.</returns>
    public bool IsSubsetOf(ColorIdentity commanderIdentity)
    {
        if (_canonicalValue.Length == 0 || _canonicalValue == "C")
        {
            return true;
        }

        if (commanderIdentity._canonicalValue == "C")
        {
            return false;
        }

        return _canonicalValue.All(commanderIdentity.Contains);
    }

    /// <summary>
    /// Formats the identity using the canonical persisted representation.
    /// </summary>
    /// <returns>A comma-separated canonical identity.</returns>
    public override string ToString() => string.Join(',', _canonicalValue.ToCharArray());
}
