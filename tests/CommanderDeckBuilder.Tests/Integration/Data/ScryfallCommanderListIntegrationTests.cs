using System.Net.Http;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

/// \brief Verifies the live Scryfall `is:commander` list against known commander-eligibility cases.
public sealed class ScryfallCommanderListIntegrationTests
{
    private const string EsikaOracleId = "92023a5d-a143-4950-a71b-d736e6b8e959";
    private const string WestvaleAbbeyOracleId = "04eeb9ad-5c59-411b-8809-db8349838588";

    /// \brief A front-face legendary creature (Esika) is a commander; a back-face-only one
    /// (Westvale Abbey // Ormendahl) is not.
    /// \return A task that completes after the live list is validated.
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Live_commander_list_follows_front_face_rule()
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var client = new ScryfallCommanderListClient(httpClient);

        var oracleIds = await client.GetCommanderOracleIdsAsync();

        Assert.True(oracleIds.Count > 1000);
        Assert.Contains(EsikaOracleId, oracleIds);
        Assert.DoesNotContain(WestvaleAbbeyOracleId, oracleIds);
    }
}
