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
        Assert.Equal("legal", mapped.OracleCard.CommanderLegality);
        Assert.Equal("card-1", mapped.CardPrinting.ScryfallId);
        Assert.Equal("oracle-1", mapped.CardPrinting.OracleId);
        Assert.Equal("https://example.test/front.png", mapped.CardPrinting.ImageUriNormal);
        Assert.Equal("https://example.test/front-art.jpg", mapped.CardPrinting.ImageUriArtCrop);
        Assert.Null(mapped.CardPrinting.BackImageUriNormal);
    }

    [Fact]
    public void Normalizes_and_deduplicates_color_identity()
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

    [Theory]
    [InlineData("token", false)]
    [InlineData("double_faced_token", false)]
    [InlineData("emblem", false)]
    [InlineData("art_series", false)]
    [InlineData("planar", false)]
    [InlineData("scheme", false)]
    [InlineData("vanguard", false)]
    [InlineData("normal", true)]
    public void Only_deck_layouts_are_deck_cards(string layout, bool expected)
    {
        var source = new ScryfallCardDocument
        {
            OracleId = "oracle-1",
            Name = expected ? "Lightning Bolt" : "Treasure",
            Layout = layout,
            TypeLine = expected ? "Instant" : "Token Artifact — Treasure",
            ScryfallId = "card-1"
        };

        Assert.Equal(expected, ScryfallCardMapper.IsDeckCard(source));
    }

    [Theory]
    [InlineData(false, null, false)]          // no top-level id and no faces
    [InlineData(true, "oracle-adrix", true)]  // reversible card: id only on its face
    [InlineData(true, "", false)]             // a face exists but carries no id either
    public void A_card_without_any_oracle_id_is_not_a_deck_card(bool hasFace, string? faceOracleId, bool expected)
    {
        var source = new ScryfallCardDocument
        {
            OracleId = string.Empty,
            Name = "Adrix and Nev, Twincasters // Adrix and Nev, Twincasters",
            Layout = "reversible_card",
            ScryfallId = "card-rev",
            Faces = hasFace
                ? [new ScryfallFaceDocument { Name = "Adrix and Nev, Twincasters", OracleId = faceOracleId }]
                : []
        };

        Assert.Equal(expected, ScryfallCardMapper.IsDeckCard(source));
    }

    [Fact]
    public void Reversible_card_uses_face_oracle_id_and_face_name()
    {
        var face = new ScryfallFaceDocument
        {
            Name = "Adrix and Nev, Twincasters",
            OracleId = "oracle-adrix",
            TypeLine = "Legendary Creature — Merfolk Dryad",
            OracleText = "Ward {2}"
        };
        var source = new ScryfallCardDocument
        {
            OracleId = string.Empty,
            Name = "Adrix and Nev, Twincasters // Adrix and Nev, Twincasters",
            Layout = "reversible_card",
            ColorIdentity = ["G", "U"],
            ScryfallId = "card-rev",
            SetCode = "sld",
            SetName = "Secret Lair Drop",
            CollectorNumber = "1",
            Rarity = "mythic",
            Faces = [face, face]
        };

        var mapped = ScryfallCardMapper.Map(source);

        Assert.Equal("oracle-adrix", mapped.OracleCard.OracleId);
        Assert.Equal("oracle-adrix", mapped.CardPrinting.OracleId);
        Assert.Equal("Adrix and Nev, Twincasters", mapped.OracleCard.Name);
        Assert.Equal("adrix and nev, twincasters", mapped.OracleCard.NormalizedName);
    }

    [Fact]
    public void Multi_faced_card_joins_face_oracle_text()
    {
        var source = new ScryfallCardDocument
        {
            OracleId = "oracle-delver",
            Name = "Delver of Secrets // Insectile Aberration",
            Layout = "transform",
            TypeLine = "Creature — Human Wizard // Creature — Human Insect",
            ColorIdentity = ["U"],
            ScryfallId = "card-delver",
            SetCode = "isd",
            SetName = "Innistrad",
            CollectorNumber = "51",
            Rarity = "common",
            Faces =
            [
                new ScryfallFaceDocument
                {
                    Name = "Delver of Secrets",
                    OracleText = "At the beginning of your upkeep, look at the top card of your library."
                },
                new ScryfallFaceDocument { Name = "Insectile Aberration", OracleText = "Flying" }
            ]
        };

        var mapped = ScryfallCardMapper.Map(source);

        Assert.Equal(
            "At the beginning of your upkeep, look at the top card of your library.\n//\nFlying",
            mapped.OracleCard.OracleText);
    }
}
