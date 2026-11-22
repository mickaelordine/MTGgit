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

    [Fact]
    public void Default_page_delay_respects_the_search_rate_limit()
    {
        // Scryfall allows 2 requests/second on /cards/search (scryfall.com/docs/api/rate-limits).
        Assert.True(ScryfallCommanderListClient.DefaultPageDelay >= TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public async Task Rate_limit_response_stops_without_retrying()
    {
        var handler = new RateLimitedHandler();
        using var httpClient = new HttpClient(handler);
        var client = new ScryfallCommanderListClient(httpClient, pageDelay: TimeSpan.Zero);

        var error = await Assert.ThrowsAsync<ScryfallRateLimitedException>(
            () => client.GetCommanderOracleIdsAsync());

        Assert.Equal(TimeSpan.FromSeconds(30), error.RetryAfter);
        Assert.Equal(1, handler.Requests);
    }

    private sealed class RateLimitedHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests++;
            var response = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(
                    """{ "object": "error", "code": "rate_limited", "status": 429 }""",
                    Encoding.UTF8,
                    "application/json")
            };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return Task.FromResult(response);
        }
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
