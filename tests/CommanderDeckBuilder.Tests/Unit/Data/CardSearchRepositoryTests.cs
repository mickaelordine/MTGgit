using CommanderDeckBuilder.Core;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

public sealed class CardSearchRepositoryTests
{
    [Fact]
    public async Task Query_returns_matching_oracle_cards_from_sqlite()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-sol", "Sol Ring", "sol ring", string.Empty, false);
        fixture.InsertCard("oracle-rock", "Arcane Signet", "arcane signet", string.Empty, false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            "sol",
            ColorIdentity.Parse("W,U,B,R,G"));

        var result = Assert.Single(results);
        Assert.Equal("oracle-sol", result.OracleId);
        Assert.Equal("Sol Ring", result.Name);
    }

    [Fact]
    public async Task Query_excludes_non_matching_names()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-sol", "Sol Ring", "sol ring", string.Empty, false);
        fixture.InsertCard("oracle-rock", "Arcane Signet", "arcane signet", string.Empty, false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            "island",
            ColorIdentity.Parse("W,U,B,R,G"));

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_query_returns_no_rows(string query)
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-sol", "Sol Ring", "sol ring", string.Empty, false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            query,
            ColorIdentity.Parse("W,U,B,R,G"));

        Assert.Empty(results);
    }

    [Fact]
    public async Task Default_search_includes_only_cards_inside_commander_identity()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-island", "Island", "island", "U", false);
        fixture.InsertCard("oracle-mountain", "Mountain", "mountain", "R", false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            "land",
            ColorIdentity.Parse("U,B"));

        var result = Assert.Single(results);
        Assert.Equal("oracle-island", result.OracleId);
    }

    [Fact]
    public async Task Disabling_color_enforcement_includes_off_color_cards()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-island", "Island", "island", "U", false);
        fixture.InsertCard("oracle-mountain", "Mountain", "mountain", "R", false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            "a",
            ColorIdentity.Parse("U,B"),
            enforceColorIdentity: false);

        Assert.Equal(
            ["oracle-island", "oracle-mountain"],
            results.Select(result => result.OracleId).ToArray());
    }

    [Fact]
    public async Task Only_commanders_excludes_non_commander_cards()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-legend", "Legendary Hero", "legendary hero", "W", true);
        fixture.InsertCard("oracle-ordinary", "Ordinary Hero", "ordinary hero", "W", false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            "hero",
            ColorIdentity.Parse("W"),
            onlyCommanders: true);

        var result = Assert.Single(results);
        Assert.Equal("oracle-legend", result.OracleId);
    }

    [Fact]
    public async Task Land_search_uses_complete_color_identity_for_basic_dual_and_triome_lands()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-plains", "Plains", "plains", "W", false);
        fixture.InsertCard("oracle-mountain", "Mountain", "mountain", "R", false);
        fixture.InsertCard("oracle-azorius", "Azorius Land", "azorius land", "U,W", false);
        fixture.InsertCard("oracle-gruul", "Gruul Land", "gruul land", "G,R", false);
        fixture.InsertCard("oracle-esper", "Esper Triome", "esper triome", "B,U,W", false);
        fixture.InsertCard("oracle-jeskai", "Jeskai Triome", "jeskai triome", "R,U,W", false);
        var repository = new OracleCardSearchRepository(fixture.Paths);
        var commander = ColorIdentity.Parse("W,U,B");

        var landResults = await repository.SearchAsync("a", commander);
        var triomeResults = await repository.SearchAsync("triome", commander);

        Assert.Equal(
            ["oracle-azorius", "oracle-plains"],
            landResults.Select(result => result.OracleId).ToArray());
        var triome = Assert.Single(triomeResults);
        Assert.Equal("oracle-esper", triome.OracleId);
    }

    [Fact]
    public async Task Colorless_identity_values_remain_valid()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-empty", "Colorless Land", "colorless land", string.Empty, false);
        fixture.InsertCard("oracle-c", "C Land", "c land", "C", false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            "land",
            ColorIdentity.Parse("R"));

        Assert.Equal(
            ["oracle-c", "oracle-empty"],
            results.Select(result => result.OracleId).ToArray());
    }

    [Fact]
    public async Task Results_contain_one_row_per_oracle_id()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-one", "Shared Name One", "shared name one", "U", false);
        fixture.InsertCard("oracle-two", "Shared Name Two", "shared name two", "U", false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            "shared",
            ColorIdentity.Parse("U"));

        Assert.Equal(
            results.Count,
            results.Select(result => result.OracleId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Malformed_persisted_color_identity_is_reported()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-invalid", "Invalid Card", "invalid card", "X", false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        await Assert.ThrowsAsync<FormatException>(
            () => repository.SearchAsync(
                "invalid",
                ColorIdentity.Parse("U")));
    }

    [Fact]
    public async Task Search_treats_sql_wildcards_as_literal_text()
    {
        using var fixture = new SearchStorageFixture();
        fixture.Initialize();
        fixture.InsertCard("oracle-plain", "Plain Card", "plain card", "U", false);
        fixture.InsertCard("oracle-underscore", "A_B Card", "a_b card", "U", false);
        var repository = new OracleCardSearchRepository(fixture.Paths);

        var results = await repository.SearchAsync(
            "_",
            ColorIdentity.Parse("U"));

        var result = Assert.Single(results);
        Assert.Equal("oracle-underscore", result.OracleId);
    }

}
