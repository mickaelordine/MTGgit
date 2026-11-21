using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class DeckCardOperationsTests
{
    [Fact]
    public void AddCardAddsEntryToRequestedZone()
    {
        var deck = CreateDeck();

        deck.AddCard("Sol Ring", "printing-sol-ring", DeckZone.Mainboard);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal("Sol Ring", entry.OracleId);
        Assert.Equal("printing-sol-ring", entry.SelectedPrintingScryfallId);
        Assert.Equal(DeckZone.Mainboard, entry.Zone);
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void AddingSamePrintingIncrementsQuantity()
    {
        var deck = CreateDeck();

        deck.AddCard("Basic Land", "printing-1", DeckZone.Mainboard);
        deck.AddCard("Basic Land", "printing-1", DeckZone.Mainboard);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal(2, entry.Quantity);
    }

    [Fact]
    public void IncrementAndDecrementChangeQuantity()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard);

        deck.IncrementCard("printing-1");
        deck.DecrementCard("printing-1");

        var entry = Assert.Single(deck.Entries);
        Assert.Equal(1, entry.Quantity);
    }

    [Fact]
    public void DecrementingLastCopyRemovesEntry()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard);

        deck.DecrementCard("printing-1");

        Assert.Empty(deck.Entries);
    }

    [Fact]
    public void RemoveCardDeletesEntry()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard);

        deck.RemoveCard("printing-1");

        Assert.Empty(deck.Entries);
    }

    [Fact]
    public void RemoveCardDeletesEntryForMoreThanOneCopy()
    {
        var deck = CreateDeck();
        deck.AddCard("Basic Land", "printing-1", DeckZone.Mainboard);
        deck.AddCard("Basic Land", "printing-1", DeckZone.Mainboard);

        deck.RemoveCard("printing-1");

        Assert.Empty(deck.Entries);
    }
    

    [Theory]
    [InlineData(DeckZone.Mainboard)]
    [InlineData(DeckZone.Sideboard)]
    [InlineData(DeckZone.Considering)]
    public void MoveCardChangesZoneWithoutChangingCardData(DeckZone destination)
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Ramp"]);

        deck.MoveCard("printing-1", destination);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal(destination, entry.Zone);
        Assert.Equal("printing-1", entry.SelectedPrintingScryfallId);
        Assert.Equal(1, entry.Quantity);
        Assert.Equal(["#Ramp"], entry.Tags);
    }

    [Fact]
    public void ChangePrintingPreservesZoneQuantityAndTags()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Sideboard, ["#Tutor"]);

        deck.ChangePrinting("printing-1", "printing-2");

        var entry = Assert.Single(deck.Entries);
        Assert.Equal("printing-2", entry.SelectedPrintingScryfallId);
        Assert.Equal(DeckZone.Sideboard, entry.Zone);
        Assert.Equal(1, entry.Quantity);
        Assert.Equal(["#Tutor"], entry.Tags);
    }

    [Fact]
    public void ChangePrintingMergesWhenTargetPrintingAlreadyExists()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Ramp"]);
        deck.AddCard("Card", "printing-2", DeckZone.Sideboard, ["#Tutor"]);

        deck.ChangePrinting("printing-1", "printing-2");

        var entry = Assert.Single(deck.Entries);
        Assert.Equal("printing-2", entry.SelectedPrintingScryfallId);
        Assert.Equal(DeckZone.Sideboard, entry.Zone);
        Assert.Equal(2, entry.Quantity);
        Assert.Equal(["#Ramp", "#Tutor"], entry.Tags);
    }

    [Fact]
    public void ChangePrintingRejectsAnotherOracleCard()
    {
        var deck = CreateDeck();
        deck.AddCard("Card A", "printing-1", DeckZone.Mainboard);
        deck.AddCard("Card B", "printing-2", DeckZone.Mainboard);

        Assert.Throws<InvalidOperationException>(
            () => deck.ChangePrinting("printing-1", "printing-2"));
        Assert.Equal(2, deck.Entries.Count);
    }

    [Fact]
    public void AddAndRemoveTagUpdateEntryTags()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Considering);

        deck.AddTag("printing-1", "#Wincon");
        deck.RemoveTag("printing-1", "#Wincon");

        Assert.Empty(Assert.Single(deck.Entries).Tags);
    }

    [Fact]
    public void DirectEntryDecrementDoesNotCreateNegativeQuantity()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard);

        Assert.False(entry.DecrementQuantity());
        Assert.Throws<InvalidOperationException>(() => entry.DecrementQuantity());
    }

    private static Deck CreateDeck()
    {
        return Deck.Create(
            new DeckCommander(
                "Test Commander",
                ColorIdentity.Parse("W,U"),
                DeckCommanderMechanic.Primary));
    }
}
