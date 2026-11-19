using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class CardSearchDomainTests
{
    [Fact]
    public void Colorless_card_identity_is_valid_for_any_commander()
    {
        var card = new CardSearchCandidate("oracle-1", "Wastes", string.Empty, false);
        var commander = ColorIdentity.Parse("W,U,B,R,G");

        var isEligible = CardSearchEligibility.IsEligible(
            card,
            commander,
            enforceColorIdentity: true,
            onlyCommanders: false);

        Assert.True(isEligible);
    }

    [Fact]
    public void Explicit_colorless_identity_is_valid_for_any_commander()
    {
        var card = new CardSearchCandidate("oracle-c", "Colorless Land", "C", false);
        var commander = ColorIdentity.Parse("W,U,B,R,G");

        var isEligible = CardSearchEligibility.IsEligible(
            card,
            commander,
            enforceColorIdentity: true,
            onlyCommanders: false);

        Assert.True(isEligible);
    }

    [Fact]
    public void Card_identity_contained_by_commander_identity_is_valid()
    {
        var card = new CardSearchCandidate("oracle-2", "Dimir Card", "U,B", false);
        var commander = ColorIdentity.Parse("U,B,G");

        var isEligible = CardSearchEligibility.IsEligible(
            card,
            commander,
            enforceColorIdentity: true,
            onlyCommanders: false);

        Assert.True(isEligible);
    }

    [Fact]
    public void Card_identity_containing_unavailable_color_is_invalid()
    {
        var card = new CardSearchCandidate("oracle-3", "Izzet Card", "U,R", false);
        var commander = ColorIdentity.Parse("U,B,G");

        var isEligible = CardSearchEligibility.IsEligible(
            card,
            commander,
            enforceColorIdentity: true,
            onlyCommanders: false);

        Assert.False(isEligible);
    }

    [Fact]
    public void Disabling_color_enforcement_allows_an_off_color_card()
    {
        var card = new CardSearchCandidate("oracle-4", "Boros Card", "W,R", false);
        var commander = ColorIdentity.Parse("U,B,G");

        var isEligible = CardSearchEligibility.IsEligible(
            card,
            commander,
            enforceColorIdentity: false,
            onlyCommanders: false);

        Assert.True(isEligible);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Search_request_rejects_non_positive_limits(int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardSearchRequest(
                "sol",
                ColorIdentity.Parse("R"),
                enforceColorIdentity: true,
                onlyCommanders: false,
                limit));
    }

    [Fact]
    public void Search_request_preserves_defaults_and_only_commanders_flag()
    {
        var request = new CardSearchRequest(
            "commander",
            ColorIdentity.Parse("W,U"),
            onlyCommanders: true);

        Assert.Equal(50, request.Limit);
        Assert.True(request.EnforceColorIdentity);
        Assert.True(request.OnlyCommanders);
    }

    [Fact]
    public void Color_identity_snapshots_caller_owned_colors()
    {
        var colors = new List<char> { 'U', 'B' };
        var identity = ColorIdentity.From(colors);

        colors.Clear();

        Assert.Equal("B,U", identity.ToString());
    }

    [Fact]
    public void Search_service_exposes_the_requested_async_contract()
    {
        var method = typeof(ICardSearchService).GetMethod(nameof(ICardSearchService.SearchCardsAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<IReadOnlyList<CardSearchResult>>), method!.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(5, parameters.Length);
        Assert.Equal(true, parameters[2].DefaultValue);
        Assert.Equal(false, parameters[3].DefaultValue);
        Assert.Equal(50, parameters[4].DefaultValue);
    }

    [Fact]
    public void Search_result_carries_oracle_level_fields()
    {
        var result = new CardSearchResult(
            "oracle-5",
            "Sol Ring",
            "sol ring",
            string.Empty,
            false);

        Assert.Equal("oracle-5", result.OracleId);
        Assert.Equal("Sol Ring", result.Name);
        Assert.Equal("sol ring", result.NormalizedName);
        Assert.Equal(string.Empty, result.ColorIdentity);
        Assert.False(result.IsPossibleCommander);
    }
}
