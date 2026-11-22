using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using CommanderDeckBuilder.Core;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

/// <summary>
/// Verifies card search against live Scryfall data persisted into SQLite.
/// </summary>
public sealed class CardSearchScryfallIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Named_scryfall_cards_are_imported_and_searchable()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        using var httpClient = CreateHttpClient();

        var service = await CreateSearchServiceAsync(
            fixture,
            httpClient,
            "Sol Ring",
            "Sol Talisman");
        var results = await service.SearchCardsAsync(
            "sol",
            ColorIdentity.Parse("U"));

        Assert.Equal(
            ["Sol Ring", "Sol Talisman"],
            results.Select(result => result.Name).ToArray());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Named_scryfall_cards_use_complete_color_identity_filtering_finding()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        using var httpClient = CreateHttpClient();

        var service = await CreateSearchServiceAsync(
            fixture,
            httpClient,
            "Island",
            "Mountain");
        var results = await service.SearchCardsAsync(
            "land",
            ColorIdentity.Parse("U"));

        var result = Assert.Single(results);
        Assert.Equal("Island", result.Name);
        Assert.Equal("U", result.ColorIdentity);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Named_scryfall_cards_use_complete_color_identity_filtering_not_finding()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        using var httpClient = CreateHttpClient();

        var service = await CreateSearchServiceAsync(
            fixture,
            httpClient,
            "Island",
            "Mountain");
        var results = await service.SearchCardsAsync(
            "Mount",
            ColorIdentity.Parse("U"));

        Assert.Equal(0, results.Count);
    }

    private static async Task<CardSearchService> CreateSearchServiceAsync(
        SearchStorageFixture fixture,
        HttpClient httpClient,
        params string[] names)
    {
        var cards = new List<MappedScryfallCard>();
        foreach (var name in names)
        {
            var card = await GetNamedCardAsync(httpClient, name);
            cards.Add(ScryfallCardMapper.Map(card));
        }

        await ImportAsync(
            fixture.Paths,
            cards.ToArray());
        return new CardSearchService(new OracleCardSearchRepository(fixture.Paths));
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MTGgit/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return client;
    }

    private static async Task<ScryfallCardDocument> GetNamedCardAsync(
        HttpClient httpClient,
        string name)
    {
        var endpoint = $"https://api.scryfall.com/cards/named?exact={Uri.EscapeDataString(name)}";
        var card = await httpClient.GetFromJsonAsync<NamedCard>(endpoint);
        if (card is null)
        {
            throw new InvalidDataException($"Scryfall returned no card for '{name}'.");
        }

        return new ScryfallCardDocument
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
    }

    private static async Task ImportAsync(
        LocalStoragePaths paths,
        params MappedScryfallCard[] cards)
    {
        var importer = new ScryfallBulkImporter(paths);
        await importer.ImportAsync(
            Enumerate(cards),
            new HashSet<string>(),
            DateTimeOffset.UtcNow);
    }

    private static async IAsyncEnumerable<MappedScryfallCard> Enumerate(
        IReadOnlyList<MappedScryfallCard> cards,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var card in cards)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return card;
        }

        await Task.CompletedTask;
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
