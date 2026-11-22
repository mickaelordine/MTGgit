using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class CardSearchRankingTests
{
    [Fact]
    public void Exact_normalized_match_ranks_first()
    {
        var candidates = new[]
        {
            Candidate("oracle-prefix", "Sol Ringing", "sol ringing"),
            Candidate("oracle-exact", "Sol Ring", "sol ring")
        };

        var ranked = CardSearchRanking.Rank(candidates, "sol ring");

        Assert.Equal("oracle-exact", ranked[0].OracleId);
    }

    [Fact]
    public void Exact_match_ignores_diacritics()
    {
        var candidates = new[]
        {
            Candidate("oracle-warlord", "Nazgul Warlord", "nazgul warlord"),
            Candidate("oracle-nazgul", "Nazgûl", string.Empty)
        };

        var ranked = CardSearchRanking.Rank(candidates, "Nazgul");

        Assert.Equal("oracle-nazgul", ranked[0].OracleId);
    }

    [Fact]
    public void Prefix_match_ranks_before_substring_match()
    {
        var candidates = new[]
        {
            Candidate("oracle-substring", "Ring of Sol", "ring of sol"),
            Candidate("oracle-prefix", "Sol Ring", "sol ring")
        };

        var ranked = CardSearchRanking.Rank(candidates, "sol");

        Assert.Equal("oracle-prefix", ranked[0].OracleId);
        Assert.Equal("oracle-substring", ranked[1].OracleId);
    }

    [Fact]
    public void Substring_match_ranks_before_fuzzy_only_candidates()
    {
        var candidates = new[]
        {
            Candidate("oracle-fuzzy", "Rong", "rong"),
            Candidate("oracle-substring", "Ring of Sol", "ring of sol")
        };

        var ranked = CardSearchRanking.Rank(candidates, "ring");

        Assert.Equal("oracle-substring", ranked[0].OracleId);
        Assert.Equal("oracle-fuzzy", ranked[1].OracleId);
    }

    [Fact]
    public void Sol_rin_ranks_sol_ring_first()
    {
        var candidates = new[]
        {
            Candidate("oracle-soul", "Soul Warden", "soul warden"),
            Candidate("oracle-sol", "Sol Ring", "sol ring")
        };

        var ranked = CardSearchRanking.Rank(candidates, "Sol Rin");

        Assert.Equal("oracle-sol", ranked[0].OracleId);
    }

    [Fact]
    public void Soll_ring_ranks_sol_ring_first()
    {
        var candidates = new[]
        {
            Candidate("oracle-soul", "Soul Ring", "soul ring"),
            Candidate("oracle-sol", "Sol Ring", "sol ring")
        };

        var ranked = CardSearchRanking.Rank(candidates, "Soll Ring");

        Assert.Equal("oracle-sol", ranked[0].OracleId);
    }

    [Fact]
    public void Fuzzy_candidates_use_edit_distance_then_name_length()
    {
        var candidates = new[]
        {
            Candidate("oracle-long", "Abxc", "abxc"),
            Candidate("oracle-short", "Xbc", "xbc")
        };

        var ranked = CardSearchRanking.Rank(candidates, "abc");

        Assert.Equal("oracle-short", ranked[0].OracleId);
        Assert.Equal("oracle-long", ranked[1].OracleId);
    }

    [Fact]
    public void Query_casing_and_surrounding_whitespace_are_normalized()
    {
        var candidates = new[]
        {
            Candidate("oracle-sol", "Sol Ring", "sol ring")
        };

        var ranked = CardSearchRanking.Rank(candidates, "  SOL RING  ");

        Assert.Equal("oracle-sol", ranked[0].OracleId);
    }

    [Fact]
    public void Equal_scores_use_normalized_name_then_oracle_id()
    {
        var candidates = new[]
        {
            Candidate("oracle-z", "Abd", "abd"),
            Candidate("oracle-a", "Abd", "abd")
        };

        var ranked = CardSearchRanking.Rank(candidates, "abc");

        Assert.Equal("oracle-a", ranked[0].OracleId);
        Assert.Equal("oracle-z", ranked[1].OracleId);
    }

    [Fact]
    public void Input_enumeration_order_does_not_change_ranking()
    {
        var candidates = new[]
        {
            Candidate("oracle-z", "Abd", "abd"),
            Candidate("oracle-a", "Abd", "abd")
        };

        var forward = CardSearchRanking.Rank(candidates, "abc")
            .Select(candidate => candidate.OracleId)
            .ToArray();
        var reverse = CardSearchRanking.Rank(candidates.Reverse(), "abc")
            .Select(candidate => candidate.OracleId)
            .ToArray();

        Assert.Equal(forward, reverse);
    }

    private static CardSearchCandidate Candidate(
        string oracleId,
        string name,
        string normalizedName)
    {
        return new CardSearchCandidate(
            oracleId,
            name,
            string.Empty,
            false,
            normalizedName);
    }
}
