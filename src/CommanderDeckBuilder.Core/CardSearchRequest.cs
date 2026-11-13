namespace CommanderDeckBuilder.Core;

/// <summary>
/// Immutable options for a card search.
/// </summary>
public sealed record CardSearchRequest
{
    /// <summary>
    /// Creates an immutable search request and validates its limit.
    /// </summary>
    /// <param name="query">The user's card-name query.</param>
    /// <param name="commanderIdentity">The active commander's color identity.</param>
    /// <param name="enforceColorIdentity">Whether color filtering is enabled.</param>
    /// <param name="onlyCommanders">Whether only possible commanders are requested.</param>
    /// <param name="limit">The maximum number of results.</param>
    public CardSearchRequest(
        string query,
        ColorIdentity commanderIdentity,
        bool enforceColorIdentity = true,
        bool onlyCommanders = false,
        int limit = 50)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        Query = query;
        CommanderIdentity = commanderIdentity;
        EnforceColorIdentity = enforceColorIdentity;
        OnlyCommanders = onlyCommanders;
        Limit = limit;
    }

    /// <summary>
    /// Gets the user's card-name query.
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// Gets the active commander's color identity.
    /// </summary>
    public ColorIdentity CommanderIdentity { get; }

    /// <summary>
    /// Gets whether color-identity filtering is enabled.
    /// </summary>
    public bool EnforceColorIdentity { get; }

    /// <summary>
    /// Gets whether only possible commanders are requested.
    /// </summary>
    public bool OnlyCommanders { get; }

    /// <summary>
    /// Gets the maximum number of results.
    /// </summary>
    public int Limit { get; }
}
