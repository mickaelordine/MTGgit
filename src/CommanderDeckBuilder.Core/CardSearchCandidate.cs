namespace CommanderDeckBuilder.Core;

/// <summary>
/// Minimal oracle-card data needed for search eligibility.
/// </summary>
public sealed record CardSearchCandidate(
    string OracleId,
    string Name,
    string ColorIdentity,
    bool IsPossibleCommander,
    string NormalizedName = ""
    );
