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
        Assert.Equal(["#ramp"], entry.Tags);
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
        Assert.Equal(["#tutor"], entry.Tags);
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
        Assert.Equal(["#ramp", "#tutor"], entry.Tags);
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
    public void ConstructorNormalizesSortsAndDeduplicatesTags()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard,
            ["  #  Wincon  ", "#Ramp", "#wincon", "#!Ramp"]);

        Assert.Equal(["#!ramp", "#wincon"], entry.Tags);
    }

    [Fact]
    public void AddTagNormalizesWhitespaceCaseAndPrefix()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard);

        entry.AddTag("  #!  Draw  ");

        Assert.Equal(["#!draw"], entry.Tags);
    }

    [Fact]
    public void AddTagTreatsEquivalentCaseAndWhitespaceAsDuplicate()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard,
            ["#Ramp"]);

        Assert.False(entry.AddTag("  #  RAMP  "));
        Assert.Equal(["#ramp"], entry.Tags);
    }

    [Fact]
    public void AddTagFromLocalToGlobalUpgrade()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard,
            ["#Ramp"]);

        Assert.True(entry.AddTag("  #!  RAMP  "));
        Assert.Equal(["#!ramp"], entry.Tags);
    }

    [Fact]
    public void AddingGlobalTagUpgradesExistingLocalTag()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard);

        entry.AddTag("#Ramp");
        entry.AddTag("#!Ramp");

        Assert.Equal(["#!ramp"], entry.Tags);
    }

    [Fact]
    public void RemoveTagNormalizesInputAndRemovesExistingTag()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard,
            ["#Ramp"]);

        Assert.True(entry.RemoveTag("  #  RAMP  "));
        Assert.Empty(entry.Tags);
    }

    [Fact]
    public void RemoveTagIsNoOpWhenTagIsMissing()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard,
            ["#Ramp"]);

        Assert.False(entry.RemoveTag("#Draw"));
        Assert.Equal(["#ramp"], entry.Tags);
    }

    [Fact]
    public void NewDeckIsClean()
    {
        var deck = CreateDeck();

        Assert.False(deck.IsDirty);
    }

    [Fact]
    public void AddingNewTagMarksDeckDirty()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard);

        deck.AddTag("printing-1", "#Ramp");

        Assert.True(deck.IsDirty);
    }

    [Fact]
    public void DuplicateTagAndMissingTagDoNotChangeDirtyState()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Ramp"]);

        deck.AddTag("printing-1", "#RAMP");
        deck.RemoveTag("printing-1", "#Draw");

        Assert.False(deck.IsDirty);
    }

    [Fact]
    public void AddingExistingGlobalTagIsNoOpAndKeepsTag()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#!Ramp"]);

        deck.AddTag("printing-1", "#!RAMP");

        Assert.Equal(["#!ramp"], Assert.Single(deck.Entries).Tags);
        Assert.False(deck.IsDirty);
    }

    [Fact]
    public void AddingLocalTagWhileGlobalTwinPresentIsNoOp()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#!Ramp"]);
        deck.MarkAsClean();

        deck.AddTag("printing-1", "#Ramp");

        Assert.Equal(["#!ramp"], Assert.Single(deck.Entries).Tags);
        Assert.False(deck.IsDirty);
    }

    [Fact]
    public void RemovingExistingTagMarksDeckDirty()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Ramp"]);

        deck.RemoveTag("printing-1", "#Ramp");

        Assert.True(deck.IsDirty);
    }

    [Fact]
    public void MarkAsCleanClearsDirtyState()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard);
        deck.AddTag("printing-1", "#Ramp");

        deck.MarkAsClean();

        Assert.False(deck.IsDirty);
    }

    [Fact]
    public void MergeFromKeepsTagsNormalizedAndSorted()
    {
        var target = new DeckCardEntry(
            "Card",
            "printing-target",
            1,
            DeckZone.Mainboard,
            ["#Draw"]);
        var source = new DeckCardEntry(
            "Card",
            "printing-source",
            1,
            DeckZone.Sideboard,
            ["#  Wincon"]);

        target.MergeFrom(source);

        Assert.Equal(["#draw", "#wincon"], target.Tags);
    }

    [Fact]
    public void AddingGlobalTagUpgradesMatchingLocalTag()
    {
        var entry = new DeckCardEntry(
            "Card",
            "printing-1",
            1,
            DeckZone.Mainboard,
            ["#Ramp"]);

        Assert.True(entry.AddTag("#!Ramp"));
        Assert.Equal(["#!ramp"], entry.Tags);
    }

    [Fact]
    public void ReaddingPrintingWithNewTagMarksDeckDirty()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard);
        deck.MarkAsClean();

        deck.AddCard(
            "Card",
            "printing-1",
            DeckZone.Sideboard,
            ["#Ramp"]);

        Assert.True(deck.IsDirty);
        Assert.Equal(["#ramp"], Assert.Single(deck.Entries).Tags);
    }

    [Fact]
    public void PrintingMergeWithNewTagMarksDeckDirty()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard);
        deck.AddCard("Card", "printing-2", DeckZone.Sideboard, ["#Wincon"]);
        deck.MarkAsClean();

        deck.ChangePrinting("printing-2", "printing-1");

        Assert.True(deck.IsDirty);
        Assert.Equal(["#wincon"], Assert.Single(deck.Entries).Tags);
    }

    [Fact]
    public void TagChangesPreserveCardIdentityZoneQuantityAndPrinting()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Sideboard);

        deck.AddTag("printing-1", "#Ramp");
        deck.RemoveTag("printing-1", "#Ramp");
        var entry = Assert.Single(deck.Entries);

        Assert.Equal("Card", entry.OracleId);
        Assert.Equal("printing-1", entry.SelectedPrintingScryfallId);
        Assert.Equal(DeckZone.Sideboard, entry.Zone);
        Assert.Equal(1, entry.Quantity);
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
