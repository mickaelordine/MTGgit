using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice2ScryfallBulkMetadataTests
{
    [Fact]
    public async Task Reads_default_cards_download_uri_and_updated_at()
    {
        using var httpClient = new HttpClient(new StubHandler("""
            {
              "download_uri": "https://data.example.test/default-cards.json",
              "updated_at": "2024-01-02T03:04:05.000Z"
            }
            """));
        var client = new ScryfallBulkMetadataClient(httpClient);

        var metadata = await client.GetDefaultCardsMetadataAsync();

        Assert.Equal(
            new Uri("https://data.example.test/default-cards.json"),
            metadata.DownloadUri);
        Assert.Equal(
            DateTimeOffset.Parse("2024-01-02T03:04:05.000Z"),
            metadata.UpdatedAt);
    }

    [Fact]
    public async Task Rejects_a_non_absolute_download_uri()
    {
        using var httpClient = new HttpClient(new StubHandler("""
            {
              "download_uri": "/default-cards.json",
              "updated_at": "2024-01-02T03:04:05.000Z"
            }
            """));
        var client = new ScryfallBulkMetadataClient(httpClient);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => client.GetDefaultCardsMetadataAsync());
    }

    private sealed class StubHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
