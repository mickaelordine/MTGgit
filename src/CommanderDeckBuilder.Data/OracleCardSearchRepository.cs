using CommanderDeckBuilder.Core;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CommanderDeckBuilder.Data;

/// <summary>
/// Reads oracle-card candidates from the local SQLite catalog.
/// </summary>
public sealed class OracleCardSearchRepository
{
    private readonly LocalStoragePaths _paths;

    /// <summary>
    /// Creates a repository using the configured local database.
    /// </summary>
    /// <param name="paths">The local storage paths containing the database.</param>
    public OracleCardSearchRepository(LocalStoragePaths paths)
    {
        _paths = paths;
    }

    /// <summary>
    /// Retrieves name-matching oracle cards that pass commander eligibility rules.
    /// </summary>
    /// <param name="query">The name fragment to search for.</param>
    /// <param name="commanderIdentity">The active commander's color identity.</param>
    /// <param name="enforceColorIdentity">Whether color filtering is enabled.</param>
    /// <param name="onlyCommanders">Whether only possible commanders are returned.</param>
    /// <returns>The eligible oracle-card candidates.</returns>
    public async Task<IReadOnlyList<CardSearchCandidate>> SearchAsync(
        string query,
        ColorIdentity commanderIdentity,
        bool enforceColorIdentity = true,
        bool onlyCommanders = false)
    {
        ArgumentNullException.ThrowIfNull(query);

        var normalizedQuery = query.Trim().ToLowerInvariant();
        if (normalizedQuery.Length == 0)
        {
            return [];
        }

        var escapedQuery = EscapeLikePattern(normalizedQuery);
        await using var connection = new SqliteConnection(_paths.ConnectionString);
        await connection.OpenAsync();
        var rows = await connection.QueryAsync<SearchRow>(
            """
            SELECT
                OracleId,
                Name,
                NormalizedName,
                ColorIdentity,
                IsPossibleCommander
            FROM OracleCards
            WHERE NormalizedName LIKE @Pattern ESCAPE '\'
            ORDER BY NormalizedName, OracleId
            """,
            new { Pattern = $"%{escapedQuery}%" });

        return rows
            .Select(row => new CardSearchCandidate(
                row.OracleId,
                row.Name,
                row.ColorIdentity,
                row.IsPossibleCommander != 0,
                row.NormalizedName))
            .Where(card => CardSearchEligibility.IsEligible(
                card,
                commanderIdentity,
                enforceColorIdentity,
                onlyCommanders))
            .ToArray();
    }

    /// <summary>
    /// Escapes SQLite LIKE metacharacters so user input is treated literally.
    /// </summary>
    /// <param name="value">The normalized query fragment.</param>
    /// <returns>The escaped LIKE pattern fragment.</returns>
    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    /// <summary>
    /// Represents the SQLite row shape before converting integer flags to domain booleans.
    /// </summary>
    private sealed record SearchRow(
        string OracleId,
        string Name,
        string NormalizedName,
        string ColorIdentity,
        long IsPossibleCommander);
}
