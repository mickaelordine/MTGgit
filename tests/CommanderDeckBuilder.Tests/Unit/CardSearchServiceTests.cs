using System.IO;
using CommanderDeckBuilder.Core;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

public sealed class CardSearchServiceTests
{
    [Fact]
    public async Task Search_returns_filtered_and_ranked_results_through_public_service()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-ring", "Ring Bearer", "ring bearer", "U", false);
        fixture.InsertCard("oracle-sol", "Sol Ring", "sol ring", string.Empty, false);
        fixture.InsertCard("oracle-off-color", "Sol Talisman", "sol talisman", "R", false);
        var service = new CardSearchService(new OracleCardSearchRepository(fixture.Paths));

        var results = await service.SearchCardsAsync(
            "sol",
            ColorIdentity.Parse("U"));

        var result = Assert.Single(results);
        Assert.Equal("oracle-sol", result.OracleId);
        Assert.Equal("Sol Ring", result.Name);
        Assert.Equal("sol ring", result.NormalizedName);
    }

    [Fact]
    public async Task Search_supports_commander_selection_through_the_same_service()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-legend", "Legendary Hero", "legendary hero", "U", true);
        fixture.InsertCard("oracle-ordinary", "Ordinary Hero", "ordinary hero", "U", false);
        var service = new CardSearchService(new OracleCardSearchRepository(fixture.Paths));

        var results = await service.SearchCardsAsync(
            "hero",
            ColorIdentity.Parse("U"),
            onlyCommanders: true);

        var result = Assert.Single(results);
        Assert.Equal("oracle-legend", result.OracleId);
        Assert.True(result.IsPossibleCommander);
    }

    [Fact]
    public async Task Search_honors_default_and_explicit_limits()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-one", "Alpha Card", "alpha card", string.Empty, false);
        fixture.InsertCard("oracle-two", "Beta Card", "beta card", string.Empty, false);
        fixture.InsertCard("oracle-three", "Gamma Card", "gamma card", string.Empty, false);
        var service = new CardSearchService(new OracleCardSearchRepository(fixture.Paths));

        var defaultResults = await service.SearchCardsAsync(
            "card",
            ColorIdentity.Parse("U"));
        var limitedResults = await service.SearchCardsAsync(
            "card",
            ColorIdentity.Parse("U"),
            limit: 1);

        Assert.Equal(3, defaultResults.Count);
        Assert.Single(limitedResults);
    }

    [Fact]
    public async Task Empty_query_returns_no_results_without_opening_the_database()
    {
        var paths = new LocalStoragePaths(
            Path.Combine(
                Path.GetTempPath(),
                "CommanderDeckBuilderTests",
                Guid.NewGuid().ToString("N")));
        var service = new CardSearchService(new OracleCardSearchRepository(paths));

        var results = await service.SearchCardsAsync(
            "   ",
            ColorIdentity.Parse("U"));

        Assert.Empty(results);
    }

}
