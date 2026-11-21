using System.IO;
using System.Diagnostics;
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
        var service = CreateService(fixture);

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
        var service = CreateService(fixture);

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
        var service = CreateService(fixture);

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

    [Fact]
    public async Task Search_propagates_repository_failures()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-invalid", "Invalid Card", "invalid card", "X", false);
        var service = CreateService(fixture);

        await Assert.ThrowsAsync<FormatException>(
            () => service.SearchCardsAsync(
                "invalid",
                ColorIdentity.Parse("U")));
    }

    [Fact]
    public async Task Search_can_be_called_from_a_thread_pool_thread()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-thread", "Thread Safe Card", "thread safe card", string.Empty, false);
        var service = CreateService(fixture);

        var results = await Task.Run(
            () => service.SearchCardsAsync(
                "thread",
                ColorIdentity.Parse("U")));

        var result = Assert.Single(results);
        Assert.Equal("oracle-thread", result.OracleId);
    }

    [Fact]
    public async Task Repeated_searches_stay_under_the_performance_gate()
    {
        const int representativeCardCount = 500;
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        for (var index = 0; index < representativeCardCount; index++)
        {
            fixture.InsertCard(
                $"oracle-{index}",
                $"Representative Card {index}",
                $"representative card {index}",
                string.Empty,
                false);
        }

        var service = CreateService(fixture);
        await service.SearchCardsAsync("representative", ColorIdentity.Parse("U"));

        var stopwatch = Stopwatch.StartNew();
        for (var iteration = 0; iteration < 10; iteration++)
        {
            await service.SearchCardsAsync("representative", ColorIdentity.Parse("U"));
        }

        stopwatch.Stop();
        var averageMilliseconds = stopwatch.Elapsed.TotalMilliseconds / 10;

        Assert.True(
            averageMilliseconds < 30,
            $"Average search time was {averageMilliseconds:F2} ms for {representativeCardCount} cards.");
    }

    private static CardSearchService CreateService(SearchStorageFixture fixture)
    {
        return new CardSearchService(new OracleCardSearchRepository(fixture.Paths));
    }

}
