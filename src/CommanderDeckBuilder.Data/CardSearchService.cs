using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Data;

/// <summary>
/// Composes repository retrieval, eligibility filtering, ranking, and result limiting.
/// </summary>
public sealed class CardSearchService : ICardSearchService
{
    private readonly OracleCardSearchRepository _repository;

    /// <summary>
    /// Creates a search service backed by the local oracle-card repository.
    /// </summary>
    /// <param name="repository">The repository used to retrieve search candidates.</param>
    public CardSearchService(OracleCardSearchRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CardSearchResult>> SearchCardsAsync(
        string query,
        ColorIdentity commanderIdentity,
        bool enforceColorIdentity = true,
        bool onlyCommanders = false,
        int limit = 50)
    {
        var request = new CardSearchRequest(
            query,
            commanderIdentity,
            enforceColorIdentity,
            onlyCommanders,
            limit);

        var candidates = await _repository.SearchAsync(
            request.Query,
            request.CommanderIdentity,
            request.EnforceColorIdentity,
            request.OnlyCommanders);

        return CardSearchRanking.Rank(candidates, request.Query)
            .Take(request.Limit)
            .Select(candidate => new CardSearchResult(
                candidate.OracleId,
                candidate.Name,
                candidate.NormalizedName,
                candidate.ColorIdentity,
                candidate.IsPossibleCommander))
            .ToArray();
    }
}
