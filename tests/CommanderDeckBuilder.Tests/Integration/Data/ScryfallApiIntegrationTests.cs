using System.IO.Compression;
using System.Net.Http;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

/// \brief Verifies the live Scryfall bulk-data API contract.
public sealed class ScryfallApiIntegrationTests
{
    /// \brief Queries Scryfall metadata and parses the first card from the live bulk stream.
    /// \return A task that completes after the live API response is validated.
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Default_cards_endpoint_returns_metadata_and_cards()
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        var metadataClient = new ScryfallBulkMetadataClient(httpClient);
        var metadata = await metadataClient.GetDefaultCardsMetadataAsync();

        Assert.Equal(Uri.UriSchemeHttps, metadata.DownloadUri.Scheme);
        Assert.EndsWith(".jsonl.gz", metadata.DownloadUri.AbsolutePath);
        Assert.True(metadata.UpdatedAt > DateTimeOffset.UnixEpoch);

        using var response = await httpClient.GetAsync(
            metadata.DownloadUri,
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);

        ScryfallCardDocument? firstCard = null;
        await foreach (var card in ScryfallBulkParser.ReadJsonLinesAsync(gzipStream))
        {
            firstCard = card;
            break;
        }

        Assert.NotNull(firstCard);
        Assert.False(string.IsNullOrWhiteSpace(firstCard!.ScryfallId));
        Assert.False(string.IsNullOrWhiteSpace(firstCard.Name));
        Assert.False(string.IsNullOrWhiteSpace(firstCard.SetCode));
    }
}
