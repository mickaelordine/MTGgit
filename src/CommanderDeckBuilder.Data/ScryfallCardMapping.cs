namespace CommanderDeckBuilder.Data;

public sealed class ScryfallCardDocument
{
    public string OracleId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
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

public sealed class ScryfallFaceDocument
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> ColorIdentity { get; init; } = [];
    public string? ImageUriNormal { get; init; }
    public string? ImageUriArtCrop { get; init; }
}

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

public sealed record MappedScryfallCard(
    OracleCardRecord OracleCard,
    CardPrintingRecord CardPrinting);

public static class ScryfallCardMapper
{
    public static MappedScryfallCard Map(ScryfallCardDocument source)
    {
        var name = source.Name.Trim();
        var oracleText = source.OracleText ?? string.Empty;
        var normalizedColorIdentity = string.Join(
            ',',
            source.ColorIdentity
                .Concat(source.Faces.SelectMany(face => face.ColorIdentity))
                .Where(color => !string.IsNullOrWhiteSpace(color))
                .Select(color => color.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(color => color, StringComparer.Ordinal));
        var frontFace = source.Faces.FirstOrDefault();
        var backFace = source.Faces.Skip(1).FirstOrDefault();

        var oracleCard = new OracleCardRecord(
            source.OracleId,
            name,
            name.ToLowerInvariant(),
            source.ManaCost,
            source.Cmc,
            source.TypeLine,
            source.OracleText,
            normalizedColorIdentity,
            source.TypeLine.Contains("Legendary Creature", StringComparison.OrdinalIgnoreCase)
                || oracleText.Contains("can be your commander", StringComparison.OrdinalIgnoreCase),
            source.CommanderLegality);

        var printing = new CardPrintingRecord(
            source.ScryfallId,
            source.OracleId,
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
