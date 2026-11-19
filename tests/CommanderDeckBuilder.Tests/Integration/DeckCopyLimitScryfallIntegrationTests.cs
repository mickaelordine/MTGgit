using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CommanderDeckBuilder.Core;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

/// <summary>
/// Verifies copy-limit validation with live Scryfall oracle and type-line data.
/// </summary>
public sealed class DeckCopyLimitScryfallIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Sol_ring_is_marked_invalid_above_one_copy()
    {
        var mapped = await GetMappedCardAsync("Sol Ring");
        var deck = CreateDeck();
        AddCard(deck, mapped);
        deck.IncrementCard(mapped.CardPrinting.ScryfallId);

        new DeckValidationService().Validate(deck);

        var entry = Assert.Single(deck.Entries);
        Assert.True(entry.HasCopyLimitViolation);
        Assert.True(entry.HasDeckValidationViolation);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Basic_land_is_valid_above_one_copy()
    {
        var mapped = await GetMappedCardAsync("Snow-Covered Plains");
        var deck = CreateDeck();
        AddCard(deck, mapped);
        deck.IncrementCard(mapped.CardPrinting.ScryfallId);

        new DeckValidationService().Validate(deck);

        var entry = Assert.Single(deck.Entries);
        Assert.False(entry.HasCopyLimitViolation);
        Assert.False(entry.HasDeckValidationViolation);
    }

    [Theory]
    [InlineData("Seven Dwarves", 7)]
    [InlineData("Nazg\u00fbl", 9)]
    [InlineData("Shadowborn Apostle", 99)]
    [InlineData("Relentless Rats", 99)]
    [Trait("Category", "Integration")]
    public async Task Copy_limit_exceptions_use_live_scryfall_card_names(
        string cardName,
        int quantity)
    {
        var mapped = await GetMappedCardAsync(cardName);
        var deck = CreateDeck();
        AddCard(deck, mapped);
        for (var copy = 1; copy < quantity; copy++)
        {
            deck.IncrementCard(mapped.CardPrinting.ScryfallId);
        }

        new DeckValidationService().Validate(deck);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal(quantity, entry.Quantity);
        Assert.False(entry.HasCopyLimitViolation);
    }

    private static Deck CreateDeck()
    {
        return Deck.Create(
            new DeckCommander(
                "Test Commander",
                ColorIdentity.Parse("W,U,B,R,G"),
                DeckCommanderMechanic.Primary));
    }

    private static void AddCard(Deck deck, MappedScryfallCard mapped)
    {
        deck.AddCard(
            mapped.OracleCard.OracleId,
            mapped.CardPrinting.ScryfallId,
            DeckZone.Mainboard,
            cardName: mapped.OracleCard.Name,
            cardTypeLine: mapped.OracleCard.TypeLine);
        deck.Entries.Single().CardColorIdentity =
            ColorIdentity.Parse(mapped.OracleCard.ColorIdentity);
    }

    private static async Task<MappedScryfallCard> GetMappedCardAsync(string name)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MTGgit/1.0");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        var endpoint = $"https://api.scryfall.com/cards/named?exact={Uri.EscapeDataString(name)}";
        var card = await httpClient.GetFromJsonAsync<NamedCard>(endpoint);
        if (card is null)
        {
            throw new InvalidDataException($"Scryfall returned no card for '{name}'.");
        }

        var document = new ScryfallCardDocument
        {
            OracleId = card.OracleId ?? string.Empty,
            Name = card.Name ?? string.Empty,
            ManaCost = card.ManaCost,
            Cmc = card.Cmc,
            TypeLine = card.TypeLine ?? string.Empty,
            OracleText = card.OracleText,
            ColorIdentity = card.ColorIdentity ?? [],
            CommanderLegality = card.Legalities?.Commander,
            ScryfallId = card.ScryfallId ?? string.Empty,
            SetCode = card.SetCode ?? string.Empty,
            SetName = card.SetName ?? string.Empty,
            CollectorNumber = card.CollectorNumber ?? string.Empty,
            Rarity = card.Rarity ?? string.Empty,
            ReleasedAt = card.ReleasedAt,
            ImageUriNormal = card.ImageUris?.Normal,
            ImageUriArtCrop = card.ImageUris?.ArtCrop
        };

        return ScryfallCardMapper.Map(document);
    }

    private sealed class NamedCard
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
        public Legalities? Legalities { get; init; }

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
        public ImageUris? ImageUris { get; init; }
    }

    private sealed class Legalities
    {
        [JsonPropertyName("commander")]
        public string? Commander { get; init; }
    }

    private sealed class ImageUris
    {
        [JsonPropertyName("normal")]
        public string? Normal { get; init; }

        [JsonPropertyName("art_crop")]
        public string? ArtCrop { get; init; }
    }
}
