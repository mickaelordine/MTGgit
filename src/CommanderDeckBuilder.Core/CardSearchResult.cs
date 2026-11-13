namespace CommanderDeckBuilder.Core;

/// <summary>
/// Oracle-level data returned by card search.
/// </summary>
public sealed record CardSearchResult(
    string OracleId,
    string Name,
    string NormalizedName,
    string ColorIdentity,
    bool IsPossibleCommander
    );
