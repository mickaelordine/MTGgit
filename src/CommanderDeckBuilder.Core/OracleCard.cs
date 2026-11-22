namespace CommanderDeckBuilder.Core;

/// <summary>
/// The Oracle-level facts of a card that deck validation reads, attached to an entry as one unit.
/// </summary>
public sealed record OracleCard
{
    /// <summary>
    /// Creates the Oracle facts of a card.
    /// </summary>
    /// <param name="name">The Oracle card name.</param>
    /// <param name="typeLine">The Scryfall type line; may be empty.</param>
    /// <param name="colorIdentity">The card color identity.</param>
    public OracleCard(string name, string typeLine, ColorIdentity colorIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(typeLine);

        Name = name;
        TypeLine = typeLine;
        ColorIdentity = colorIdentity;
    }

    /// <summary>
    /// Creates the stand-in facts used before real Oracle data is attached:
    /// the oracle identifier as name, an empty type line and colorless identity.
    /// </summary>
    /// <param name="oracleId">The Scryfall oracle identifier.</param>
    /// <returns>The placeholder Oracle facts.</returns>
    internal static OracleCard Placeholder(string oracleId) =>
        new(oracleId, string.Empty, ColorIdentity.Parse("C"));

    /// <summary>Gets the Oracle card name used by copy-limit validation.</summary>
    public string Name { get; }

    /// <summary>Gets the type line used by basic-land and copy-limit validation.</summary>
    public string TypeLine { get; }

    /// <summary>Gets the color identity used by color-identity validation.</summary>
    public ColorIdentity ColorIdentity { get; }
}
