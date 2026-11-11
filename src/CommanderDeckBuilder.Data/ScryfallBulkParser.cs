using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommanderDeckBuilder.Data;

public static class ScryfallBulkParser
{
    public static async IAsyncEnumerable<ScryfallCardDocument> ReadAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var source in JsonSerializer.DeserializeAsyncEnumerable<JsonCard>(
                           stream,
                           cancellationToken: cancellationToken))
        {
            if (source is not null)
            {
                yield return ToDocument(source);
            }
        }
    }

    private static ScryfallCardDocument ToDocument(JsonCard source)
    {
        return new ScryfallCardDocument
        {
            OracleId = source.OracleId ?? string.Empty,
            Name = source.Name ?? string.Empty,
            ManaCost = source.ManaCost,
            Cmc = source.Cmc,
            TypeLine = source.TypeLine ?? string.Empty,
            OracleText = source.OracleText,
            ColorIdentity = source.ColorIdentity ?? [],
            CommanderLegality = source.Legalities?.Commander,
            ScryfallId = source.ScryfallId ?? string.Empty,
            SetCode = source.SetCode ?? string.Empty,
            SetName = source.SetName ?? string.Empty,
            CollectorNumber = source.CollectorNumber ?? string.Empty,
            Rarity = source.Rarity ?? string.Empty,
            ReleasedAt = source.ReleasedAt,
            ImageUriNormal = source.ImageUris?.Normal,
            ImageUriArtCrop = source.ImageUris?.ArtCrop,
            Faces = source.Faces?.Select(face => new ScryfallFaceDocument
            {
                Name = face.Name ?? string.Empty,
                ColorIdentity = face.ColorIdentity ?? [],
                ImageUriNormal = face.ImageUris?.Normal,
                ImageUriArtCrop = face.ImageUris?.ArtCrop
            }).ToArray() ?? []
        };
    }

    private sealed class JsonCard
    {
        [JsonPropertyName("oracle_id")]
        public string? OracleId { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("mana_cost")]
        public string? ManaCost { get; init; }

        [JsonPropertyName("cmc")]
        public decimal Cmc { get; init; }

        [JsonPropertyName("type_line")]
        public string? TypeLine { get; init; }

        [JsonPropertyName("oracle_text")]
        public string? OracleText { get; init; }

        [JsonPropertyName("color_identity")]
        public string[]? ColorIdentity { get; init; }

        [JsonPropertyName("legalities")]
        public JsonLegalities? Legalities { get; init; }

        [JsonPropertyName("id")]
        public string? ScryfallId { get; init; }

        [JsonPropertyName("set")]
        public string? SetCode { get; init; }

        [JsonPropertyName("set_name")]
        public string? SetName { get; init; }

        [JsonPropertyName("collector_number")]
        public string? CollectorNumber { get; init; }

        [JsonPropertyName("rarity")]
        public string? Rarity { get; init; }

        [JsonPropertyName("released_at")]
        public string? ReleasedAt { get; init; }

        [JsonPropertyName("image_uris")]
        public JsonImageUris? ImageUris { get; init; }

        [JsonPropertyName("card_faces")]
        public JsonFace[]? Faces { get; init; }
    }

    private sealed class JsonFace
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("color_identity")]
        public string[]? ColorIdentity { get; init; }

        [JsonPropertyName("image_uris")]
        public JsonImageUris? ImageUris { get; init; }
    }

    private sealed class JsonImageUris
    {
        [JsonPropertyName("normal")]
        public string? Normal { get; init; }

        [JsonPropertyName("art_crop")]
        public string? ArtCrop { get; init; }
    }

    private sealed class JsonLegalities
    {
        [JsonPropertyName("commander")]
        public string? Commander { get; init; }
    }
}
