using System.IO;
using System.Text;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice3ScryfallBulkParserTests
{
    [Fact]
    public async Task Enumerates_cards_from_the_bulk_json_stream()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            [
              {
                "oracle_id": "oracle-1",
                "name": "First Card",
                "type_line": "Creature",
                "color_identity": ["G"],
                "id": "card-1",
                "set": "abc",
                "set_name": "Example Set",
                "collector_number": "1",
                "rarity": "common"
              },
              {
                "oracle_id": "oracle-2",
                "name": "Second Card",
                "type_line": "Sorcery",
                "color_identity": [],
                "id": "card-2",
                "set": "abc",
                "set_name": "Example Set",
                "collector_number": "2",
                "rarity": "common"
              }
            ]
            """));

        var cards = new List<ScryfallCardDocument>();
        await foreach (var card in ScryfallBulkParser.ReadAsync(stream))
        {
            cards.Add(card);
        }

        Assert.Equal(2, cards.Count);
        Assert.Equal("oracle-1", cards[0].OracleId);
        Assert.Equal("Second Card", cards[1].Name);
    }

    [Fact]
    public async Task Parses_nested_card_faces_for_the_mapper()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""
            [
              {
                "oracle_id": "oracle-dfc",
                "name": "Front // Back",
                "type_line": "Creature",
                "id": "card-dfc",
                "set": "dfc",
                "set_name": "Double Face Set",
                "collector_number": "10",
                "rarity": "rare",
                "card_faces": [
                  {
                    "name": "Front",
                    "color_identity": ["G"],
                    "image_uris": {
                      "normal": "https://example.test/front.png",
                      "art_crop": "https://example.test/front-art.jpg"
                    }
                  },
                  {
                    "name": "Back",
                    "color_identity": ["U"],
                    "image_uris": {
                      "normal": "https://example.test/back.png"
                    }
                  }
                ]
              }
            ]
            """));

        ScryfallCardDocument? source = null;
        await foreach (var card in ScryfallBulkParser.ReadAsync(stream))
        {
            source = card;
            break;
        }

        Assert.NotNull(source);
        var mapped = ScryfallCardMapper.Map(source);

        Assert.Equal("G,U", mapped.OracleCard.ColorIdentity);
        Assert.Equal("https://example.test/back.png", mapped.CardPrinting.BackImageUriNormal);
    }
}
