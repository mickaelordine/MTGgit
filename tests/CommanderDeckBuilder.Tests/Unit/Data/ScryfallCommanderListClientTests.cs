using System.Net;
using System.Net.Http;
using System.Text;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

public sealed class ScryfallCommanderListClientTests
{
    [Fact]
    public async Task Commander_list_is_read_page_by_page()
    {
        var handler = new PagedSearchHandler();
        using var httpClient = new HttpClient(handler);
        var client = new ScryfallCommanderListClient(httpClient, pageDelay: TimeSpan.Zero);

        var oracleIds = await client.GetCommanderOracleIdsAsync();

        Assert.Equal(["oracle-esika", "oracle-estrid"], oracleIds.Order(StringComparer.Ordinal));
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("q=is%3Acommander", handler.Requests[0].Query, StringComparison.Ordinal);
        Assert.Equal(PagedSearchHandler.SecondPage, handler.Requests[1]);
    }

    private sealed class PagedSearchHandler : HttpMessageHandler
    {
        public static readonly Uri SecondPage =
            new("https://api.scryfall.com/cards/search?q=is%3Acommander&unique=cards&page=2");

        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            var body = Requests.Count == 1
                ? $$"""
                    {
                      "object": "list",
                      "has_more": true,
                      "next_page": "{{SecondPage}}",
                      "data": [ { "oracle_id": "oracle-esika", "name": "Esika, God of the Tree // The Prismatic Bridge" } ]
                    }
                    """
                : """
                    {
                      "object": "list",
                      "has_more": false,
                      "data": [
                        {
                          "name": "Estrid, the Masked // Estrid, the Masked",
                          "card_faces": [ { "oracle_id": "oracle-estrid" }, { "oracle_id": "oracle-estrid" } ]
                        }
                      ]
                    }
                    """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
