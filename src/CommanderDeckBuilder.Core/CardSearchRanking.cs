namespace CommanderDeckBuilder.Core;

/// <summary>
/// Orders oracle-card candidates by closeness to a user's name query.
/// </summary>
public static class CardSearchRanking
{
    /// <summary>
    /// Ranks candidates from exact matches through fuzzy matches.
    /// </summary>
    /// <param name="candidates">The candidates to rank.</param>
    /// <param name="query">The user's card-name query.</param>
    /// <returns>A deterministic ranked candidate list.</returns>
    public static IReadOnlyList<CardSearchCandidate> Rank(
        IEnumerable<CardSearchCandidate> candidates,
        string query)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(query);

        var normalizedQuery = CardNameNormalizer.ToSearchKey(query);
        return candidates
            .Select(candidate => new RankedCandidate(
                candidate,
                Score(candidate, normalizedQuery)))
            .OrderBy(item => item.Score.Rank)
            .ThenBy(item => item.Score.Distance)
            .ThenBy(item => item.Score.NameLength)
            .ThenBy(item => item.Candidate.NormalizedName, StringComparer.Ordinal)
            .ThenBy(item => item.Candidate.OracleId, StringComparer.Ordinal)
            .Select(item => item.Candidate)
            .ToArray();
    }

    /// <summary>
    /// Calculates the match class and fuzzy distance for one candidate.
    /// </summary>
    /// <param name="candidate">The candidate to score.</param>
    /// <param name="query">The normalized query.</param>
    /// <returns>The candidate's ranking score.</returns>
    private static RankingScore Score(
        CardSearchCandidate candidate,
        string query)
    {
        var normalizedName = GetNormalizedName(candidate);
        if (normalizedName == query)
        {
            return new RankingScore(0, 0, normalizedName.Length);
        }

        if (normalizedName.StartsWith(query, StringComparison.Ordinal))
        {
            return new RankingScore(1, 0, normalizedName.Length);
        }

        if (normalizedName.Contains(query, StringComparison.Ordinal))
        {
            return new RankingScore(2, 0, normalizedName.Length);
        }

        return new RankingScore(
            3,
            LevenshteinDistance(normalizedName, query),
            normalizedName.Length);
    }

    /// <summary>
    /// Gets the normalized name, deriving it when a caller omitted the field.
    /// </summary>
    /// <param name="candidate">The candidate to normalize.</param>
    /// <returns>The case- and diacritic-insensitive search key of the card name.</returns>
    private static string GetNormalizedName(CardSearchCandidate candidate)
    {
        return CardNameNormalizer.ToSearchKey(
            string.IsNullOrEmpty(candidate.NormalizedName) ? candidate.Name : candidate.NormalizedName);
    }

    /// <summary>
    /// Calculates the Levenshtein edit distance between two strings.
    /// </summary>
    /// <param name="left">Normalized name</param>
    /// <param name="right">user query</param>
    /// <returns>The minimum number of insertions, deletions, and substitutions.</returns>
    /// <remarks> This implementation can be optimized for performance and making it better for user experience in a O(nLog(n)) time complexity. </remarks>
    private static int LevenshteinDistance(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        var current = new int[right.Length + 1];

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var substitutionCost = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    /// <summary>
    /// Holds the ordering dimensions for one candidate.
    /// </summary>
    private readonly record struct RankingScore(int Rank, int Distance, int NameLength);

    /// <summary>
    /// Couples a candidate with its calculated ranking score.
    /// </summary>
    private sealed record RankedCandidate(
        CardSearchCandidate Candidate,
        RankingScore Score);
}
