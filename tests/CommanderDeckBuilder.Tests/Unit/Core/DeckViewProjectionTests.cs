using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class DeckViewProjectionTests
{
    [Theory]
    [InlineData("Creature — Beast", "Creature")]
    [InlineData("Instant", "Instant")]
    [InlineData("Sorcery", "Sorcery")]
    [InlineData("Artifact", "Artifact")]
    [InlineData("Enchantment", "Enchantment")]
    [InlineData("Land", "Land")]
    [InlineData("Battle", "Battle")]
    [InlineData("Planeswalker", "Planeswalker")]
    [InlineData("", "Other")]
    public void MapsCardTypeToCategory(string typeLine, string category)
    {
        var entry = CreateEntry("Card", "printing", typeLine);

        Assert.Equal(category, new DeckViewProjection().GetCategory(entry));
    }

    [Fact]
    public void MultiTypeLineUsesDeterministicPriority()
    {
        var entry = CreateEntry("Card", "printing", "Artifact Creature");

        Assert.Equal("Creature", new DeckViewProjection().GetCategory(entry));
    }

    [Fact]
    public void EnchantmentCreatureIsCategorizedAsCreature()
    {
        var entry = CreateEntry("Card", "printing", "Enchantment Creature — God");

        Assert.Equal("Creature", new DeckViewProjection().GetCategory(entry));
    }

    [Fact]
    public void UnknownTypeLineFallsBackToOther()
    {
        var entry = CreateEntry("Card", "printing", "Scheme Phenomenon");

        Assert.Equal("Other", new DeckViewProjection().GetCategory(entry));
    }

    [Fact(Skip = "Inactive exact-coding test")]
    public void TagsOffGroupsCardsByCategory()
    {
        var deck = CreateDeck();
        deck.AddCard("Bear", "printing-bear", DeckZone.Mainboard, cardTypeLine: "Creature");
        deck.AddCard("Bolt", "printing-bolt", DeckZone.Mainboard, cardTypeLine: "Instant");

        var groups = new DeckViewProjection().GetGroups(deck, tagsEnabled: false);

        Assert.Contains(groups, group => group.Name == "Creature");
        Assert.Contains(groups, group => group.Name == "Instant");
    }

    [Fact(Skip = "Inactive exact-coding test")]
    public void TagsOnGroupsTaggedCardsByTag()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing", DeckZone.Mainboard, ["#Ramp"], cardTypeLine: "Artifact");

        var groups = new DeckViewProjection().GetGroups(deck, tagsEnabled: true);

        Assert.Contains(groups, group => group.Name == "#ramp");
    }

    [Fact(Skip = "Inactive exact-coding test")]
    public void UntaggedCardsAreGroupedByCategoryUnderUntagged()
    {
        var deck = CreateDeck();
        deck.AddCard("Bear", "printing-bear", DeckZone.Mainboard, cardTypeLine: "Creature");

        var groups = new DeckViewProjection().GetGroups(deck, tagsEnabled: true);

        Assert.Contains(groups, group =>
            group.Name == "Untagged: Creature"
            && group.Entries.Count == 1);
    }

    [Fact(Skip = "Inactive exact-coding test")]
    public void MultiTaggedCardAppearsInEveryTagGroup()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing", DeckZone.Mainboard, ["#Ramp", "#Wincon"], cardTypeLine: "Artifact");

        var groups = new DeckViewProjection().GetGroups(deck, tagsEnabled: true);
        var ramp = Assert.Single(groups, group => group.Name == "#ramp");
        var wincon = Assert.Single(groups, group => group.Name == "#wincon");

        Assert.Single(ramp.Entries);
        Assert.Single(wincon.Entries);
    }

    [Fact(Skip = "Inactive exact-coding test")]
    public void FilterByNormalizedTagReturnsOnlyMatches()
    {
        var deck = CreateDeck();
        deck.AddCard("Bear", "printing-bear", DeckZone.Mainboard, ["#Ramp"]);
        deck.AddCard("Bolt", "printing-bolt", DeckZone.Mainboard, ["#Draw"]);

        var entries = new DeckViewProjection().FilterByTag(deck, "  #  RAMP ");

        Assert.Single(entries);
    }

    [Fact(Skip = "Inactive exact-coding test")]
    public void UnknownTagFilterReturnsNoCards()
    {
        var deck = CreateDeck();
        deck.AddCard("Bear", "printing-bear", DeckZone.Mainboard, ["#Ramp"]);

        Assert.Empty(new DeckViewProjection().FilterByTag(deck, "#Draw"));
    }

    [Theory(Skip = "Inactive exact-coding test")]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyDeckProducesNoGroups(bool tagsEnabled)
    {
        var deck = CreateDeck();

        Assert.Empty(new DeckViewProjection().GetGroups(deck, tagsEnabled));
    }

    private static Deck CreateDeck()
    {
        return Deck.Create(
            new DeckCommander(
                "Test Commander",
                ColorIdentity.Parse("W,U"),
                DeckCommanderMechanic.Primary));
    }

    private static DeckCardEntry CreateEntry(string oracleId, string printingId, string typeLine)
    {
        return new DeckCardEntry(
            oracleId,
            printingId,
            1,
            DeckZone.Mainboard,
            cardTypeLine: typeLine);
    }
}
