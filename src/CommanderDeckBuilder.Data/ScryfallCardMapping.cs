namespace CommanderDeckBuilder.Data;

/// \brief Contains the card and printing fields read from Scryfall.
public sealed class ScryfallCardDocument
{
    public string OracleId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Layout { get; init; } = string.Empty;
    public string? ManaCost { get; init; }
    public decimal Cmc { get; init; }
    public string TypeLine { get; init; } = string.Empty;
    public string? OracleText { get; init; }
    public IReadOnlyList<string> ColorIdentity { get; init; } = [];
    public string? CommanderLegality { get; init; }
    public string ScryfallId { get; init; } = string.Empty;
    public string SetCode { get; init; } = string.Empty;
    public string SetName { get; init; } = string.Empty;
    public string CollectorNumber { get; init; } = string.Empty;
    public string Rarity { get; init; } = string.Empty;
    public string? ReleasedAt { get; init; }
    public string? ImageUriNormal { get; init; }
    public string? ImageUriArtCrop { get; init; }
    public IReadOnlyList<ScryfallFaceDocument> Faces { get; init; } = [];
}

/// \brief Contains the fields read from one Scryfall card face.
public sealed class ScryfallFaceDocument
{
    public string Name { get; init; } = string.Empty;
    public string? OracleId { get; init; }
    public string TypeLine { get; init; } = string.Empty;
    public string? OracleText { get; init; }
    public IReadOnlyList<string> ColorIdentity { get; init; } = [];
    public string? ImageUriNormal { get; init; }
    public string? ImageUriArtCrop { get; init; }
}

/// \brief Represents the normalized oracle-level card data stored locally.
public sealed record OracleCardRecord(
    string OracleId,
    string Name,
    string NormalizedName,
    string? ManaCost,
    decimal Cmc,
    string TypeLine,
    string? OracleText,
    string ColorIdentity,
    bool IsPossibleCommander,
    string? CommanderLegality);

/// \brief Represents one released printing of an oracle card.
public sealed record CardPrintingRecord(
    string ScryfallId,
    string OracleId,
    string SetCode,
    string SetName,
    string CollectorNumber,
    string Rarity,
    string? ReleasedAt,
    string? ImageUriNormal,
    string? ImageUriArtCrop,
    string? BackImageUriNormal);

/// \brief Couples an oracle card record with one printing record.
public sealed record MappedScryfallCard(
    OracleCardRecord OracleCard,
    CardPrintingRecord CardPrinting);

/// \brief Converts Scryfall source documents into normalized persistence records.
public static class ScryfallCardMapper
{
    /// \brief Scryfall layouts of objects that can never be put in a deck
    /// (tokens, emblems, art cards, Planechase/Archenemy/Vanguard cards).
    private static readonly HashSet<string> NonDeckLayouts = new(StringComparer.OrdinalIgnoreCase)
    {
        "token",
        "double_faced_token",
        "emblem",
        "art_series",
        "planar",
        "scheme",
        "vanguard"
    };

    /// \brief Decides whether a Scryfall object is a card that can be put in a deck.
    /// \param source The Scryfall card document.
    /// \return False for non-deck layouts or when no oracle id exists on the card or any face; otherwise true.
    public static bool IsDeckCard(ScryfallCardDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return !NonDeckLayouts.Contains(source.Layout)
            && !string.IsNullOrEmpty(ResolveOracleId(source));
    }

    /// \brief Returns the card's oracle id, falling back to the first face that carries one
    /// (reversible cards carry it only on their faces).
    /// \param source The Scryfall card document.
    /// \return The oracle id, or an empty string when none exists.
    private static string ResolveOracleId(ScryfallCardDocument source)
    {
        return !HasFaceIdentity(source)
            ? source.OracleId
            : source.Faces.Select(face => face.OracleId).FirstOrDefault(id => !string.IsNullOrEmpty(id))
                ?? string.Empty;
    }

    /// \brief Tells whether the card's oracle identity lives on its faces (reversible cards
    /// have no top-level oracle id).
    /// \param source The Scryfall card document.
    /// \return True when the top-level oracle id is missing.
    private static bool HasFaceIdentity(ScryfallCardDocument source) =>
        string.IsNullOrEmpty(source.OracleId);

    /// \brief Joins the rules text of a multi-faced card's faces in face order.
    /// \param faces The card faces.
    /// \return The face texts separated by "\n//\n", or null when no face has text.
    private static string? JoinFaceOracleText(IReadOnlyList<ScryfallFaceDocument> faces)
    {
        var texts = faces
            .Select(face => face.OracleText)
            .Where(text => !string.IsNullOrEmpty(text))
            .ToArray();
        return texts.Length == 0 ? null : string.Join("\n//\n", texts);
    }

    /// \brief Maps one source card and its faces into oracle and printing records.
    /// \param source The Scryfall card document to map.
    /// \param commanderOracleIds The oracle ids Scryfall accepts as commanders (`is:commander`);
    /// a card is a possible commander only when its oracle id is in this set.
    /// \return The normalized oracle card and printing records.
    public static MappedScryfallCard Map(
        ScryfallCardDocument source,
        IReadOnlySet<string> commanderOracleIds)
    {
        ArgumentNullException.ThrowIfNull(commanderOracleIds);

        var frontFace = source.Faces.FirstOrDefault();
        var backFace = source.Faces.Skip(1).FirstOrDefault();
        var oracleId = ResolveOracleId(source);
        // Both faces of a reversible card are the same card, so the face name is the card name
        // (the top-level name is "X // X").
        var name = (HasFaceIdentity(source) && frontFace is not null
            ? frontFace.Name
            : source.Name).Trim();
        var normalizedColorIdentity = string.Join(
            ',',
            source.ColorIdentity
                .Concat(source.Faces.SelectMany(face => face.ColorIdentity))
                .Where(color => !string.IsNullOrWhiteSpace(color))
                .Select(color => color.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(color => color, StringComparer.Ordinal));

        var oracleCard = new OracleCardRecord(
            oracleId,
            name,
            name.ToLowerInvariant(),
            source.ManaCost,
            source.Cmc,
            source.TypeLine,
            source.OracleText ?? JoinFaceOracleText(source.Faces),
            normalizedColorIdentity,
            commanderOracleIds.Contains(oracleId),
            source.CommanderLegality);

        var printing = new CardPrintingRecord(
            source.ScryfallId,
            oracleId,
            source.SetCode,
            source.SetName,
            source.CollectorNumber,
            source.Rarity,
            source.ReleasedAt,
            frontFace?.ImageUriNormal ?? source.ImageUriNormal,
            frontFace?.ImageUriArtCrop ?? source.ImageUriArtCrop,
            backFace?.ImageUriNormal);

        return new MappedScryfallCard(oracleCard, printing);
    }
}
