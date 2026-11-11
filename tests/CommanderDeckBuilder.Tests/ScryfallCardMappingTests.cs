using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice1ScryfallCardMappingTests
{
    [Fact]
    public void Maps_a_single_faced_card_to_oracle_card_and_printing_records()
    {
        var source = new ScryfallCardDocument
        {
            OracleId = "oracle-1",
            Name = "  Example Commander  ",
            ManaCost = "{1}{U}{B}",
            Cmc = 3,
            TypeLine = "Legendary Creature — Human",
            OracleText = "Flying",
            ColorIdentity = ["b", "u"],
            CommanderLegality = "legal",
            ScryfallId = "card-1",
            SetCode = "abc",
            SetName = "Example Set",
            CollectorNumber = "1",
            Rarity = "rare",
            ReleasedAt = "2024-01-02",
            ImageUriNormal = "https://example.test/front.png",
            ImageUriArtCrop = "https://example.test/front-art.jpg"
        };

        var mapped = ScryfallCardMapper.Map(source);

        Assert.Equal("oracle-1", mapped.OracleCard.OracleId);
        Assert.Equal("Example Commander", mapped.OracleCard.Name);
        Assert.Equal("example commander", mapped.OracleCard.NormalizedName);
        Assert.Equal("B,U", mapped.OracleCard.ColorIdentity);
        Assert.True(mapped.OracleCard.IsPossibleCommander);
        Assert.Equal("legal", mapped.OracleCard.CommanderLegality);
        Assert.Equal("card-1", mapped.CardPrinting.ScryfallId);
        Assert.Equal("oracle-1", mapped.CardPrinting.OracleId);
        Assert.Equal("https://example.test/front.png", mapped.CardPrinting.ImageUriNormal);
        Assert.Equal("https://example.test/front-art.jpg", mapped.CardPrinting.ImageUriArtCrop);
        Assert.Null(mapped.CardPrinting.BackImageUriNormal);
    }

    [Fact]
    public void Marks_a_card_possible_commander_when_oracle_text_allows_it()
    {
        var source = new ScryfallCardDocument
        {
            OracleId = "oracle-2",
            Name = "Example Background",
            TypeLine = "Enchantment",
            OracleText = "This card can be your commander.",
            ColorIdentity = ["g", "g"],
            ScryfallId = "card-2",
            SetCode = "abc",
            SetName = "Example Set",
            CollectorNumber = "2",
            Rarity = "uncommon"
        };

        var mapped = ScryfallCardMapper.Map(source);

        Assert.True(mapped.OracleCard.IsPossibleCommander);
        Assert.Equal("G", mapped.OracleCard.ColorIdentity);
    }

    [Fact]
    public void Maps_front_and_back_images_and_combines_face_color_identity()
    {
        var source = new ScryfallCardDocument
        {
            OracleId = "oracle-dfc",
            Name = "Front // Back",
            TypeLine = "Creature — Human",
            ColorIdentity = [],
            ScryfallId = "card-dfc",
            SetCode = "dfc",
            SetName = "Double Face Set",
            CollectorNumber = "10",
            Rarity = "rare",
            Faces =
            [
                new ScryfallFaceDocument
                {
                    Name = "Front",
                    ColorIdentity = ["g"],
                    ImageUriNormal = "https://example.test/front.png",
                    ImageUriArtCrop = "https://example.test/front-art.jpg"
                },
                new ScryfallFaceDocument
                {
                    Name = "Back",
                    ColorIdentity = ["u"],
                    ImageUriNormal = "https://example.test/back.png",
                    ImageUriArtCrop = "https://example.test/back-art.jpg"
                }
            ]
        };

        var mapped = ScryfallCardMapper.Map(source);

        Assert.Equal("G,U", mapped.OracleCard.ColorIdentity);
        Assert.Equal("https://example.test/front.png", mapped.CardPrinting.ImageUriNormal);
        Assert.Equal("https://example.test/front-art.jpg", mapped.CardPrinting.ImageUriArtCrop);
        Assert.Equal("https://example.test/back.png", mapped.CardPrinting.BackImageUriNormal);
    }
}
