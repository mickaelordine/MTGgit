using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class DeckValidationTests
{
    [Fact]
    public void CardIdentityContainedByCommanderIsValid()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("Dimir Card", "printing-1", DeckZone.Mainboard, oracleCard: new OracleCard("Dimir Card", "", ColorIdentity.Parse("U,B")));

        var service = new DeckValidationService();
        service.Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void AddingCardWithOracleCardValidatesColorIdentityImmediately()
    {
        var deck = CreateDeck("U,B");

        deck.AddCard(
            "oracle-bolt",
            "printing-1",
            DeckZone.Mainboard,
            oracleCard: new OracleCard("Lightning Bolt", "Instant", ColorIdentity.Parse("R")));

        Assert.True(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void ReaddingPrintingWithOracleCardReplacesAllFacts()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("oracle-bolt", "printing-1", DeckZone.Mainboard);

        deck.AddCard(
            "oracle-bolt",
            "printing-1",
            DeckZone.Mainboard,
            oracleCard: new OracleCard("Lightning Bolt", "Instant", ColorIdentity.Parse("R")));

        var entry = Assert.Single(deck.Entries);
        Assert.Equal("Lightning Bolt", entry.CardName);
        Assert.Equal("Instant", entry.CardTypeLine);
        Assert.Equal(ColorIdentity.Parse("R"), entry.CardColorIdentity);
        Assert.True(entry.HasColorIdentityViolation);
        Assert.Equal(2, entry.Quantity);
    }

    [Fact]
    public void ReaddingPrintingWithoutOracleCardKeepsExistingFacts()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard(
            "oracle-bolt",
            "printing-1",
            DeckZone.Mainboard,
            oracleCard: new OracleCard("Lightning Bolt", "Instant", ColorIdentity.Parse("R")));

        deck.AddCard("oracle-bolt", "printing-1", DeckZone.Mainboard);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal("Lightning Bolt", entry.CardName);
        Assert.Equal("Instant", entry.CardTypeLine);
        Assert.Equal(ColorIdentity.Parse("R"), entry.CardColorIdentity);
        Assert.True(entry.HasColorIdentityViolation);
    }

    [Fact]
    public void CardOutsideCommanderIdentityIsMarkedInvalid()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("Red Card", "printing-1", DeckZone.Mainboard, oracleCard: new OracleCard("Red Card", "", ColorIdentity.Parse("R")));

        new DeckValidationService().Validate(deck);

        Assert.True(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void ColorlessCardIsValidForColoredCommander()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("Wastes", "printing-1", DeckZone.Sideboard, oracleCard: new OracleCard("Wastes", "", ColorIdentity.Parse("C")));

        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void ChangingCommanderRevalidatesExistingCards()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("Red Card", "printing-1", DeckZone.Mainboard, oracleCard: new OracleCard("Red Card", "", ColorIdentity.Parse("R")));

        new DeckValidationService().Validate(deck);
        Assert.True(Assert.Single(deck.Entries).HasColorIdentityViolation);

        deck.ChangePrimaryCommander(
            new DeckCommander("Five Color", ColorIdentity.Parse("W,U,B,R,G"), DeckCommanderMechanic.Primary));
        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Theory]
    [InlineData(DeckZone.Commander)]
    [InlineData(DeckZone.Mainboard)]
    [InlineData(DeckZone.Sideboard)]
    [InlineData(DeckZone.Considering)]
    public void EveryZoneIsRevalidatedAfterCommanderChange(DeckZone zone)
    {
        var deck = CreateDeck("U");
        deck.AddCard("Black Card", "printing-1", zone, oracleCard: new OracleCard("Black Card", "", ColorIdentity.Parse("B")));

        new DeckValidationService().Validate(deck);
        Assert.True(Assert.Single(deck.Entries).HasColorIdentityViolation);

        deck.ChangePrimaryCommander(
            new DeckCommander("Dimir", ColorIdentity.Parse("U,B"), DeckCommanderMechanic.Primary));
        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void DeckSizeIsValidOnlyAtExactlyOneHundredCards()
    {
        var deck = CreateDeck("W");
        deck.AddCard("Commander", "printing-c", DeckZone.Commander);
        deck.AddCard("Mainboard", "printing-main", DeckZone.Mainboard);
        for (var copy = 1; copy < 98; copy++)
        {
            deck.IncrementCard("printing-main");
        }

        var deckSizeEntry = Assert.Single(deck.Entries, entry => entry.Zone == DeckZone.Mainboard);
        Assert.False(Deck.IsDeckSizeValid(deck.Entries));

        deckSizeEntry.IncrementQuantity();
        Assert.True(Deck.IsDeckSizeValid(deck.Entries));

        deckSizeEntry.IncrementQuantity();
        Assert.False(Deck.IsDeckSizeValid(deck.Entries));
    }

    [Fact]
    public void ChangingSecondaryCommanderRevalidatesExistingCards()
    {
        var deck = Deck.Create(
            new DeckCommander("Partner A", ColorIdentity.Parse("U"), DeckCommanderMechanic.Partner),
            new DeckCommander("Partner B", ColorIdentity.Parse("B"), DeckCommanderMechanic.Partner));
        deck.AddCard("Red Card", "printing-1", DeckZone.Mainboard, oracleCard: new OracleCard("Red Card", "", ColorIdentity.Parse("R")));

        new DeckValidationService().Validate(deck);
        Assert.True(Assert.Single(deck.Entries).HasColorIdentityViolation);

        deck.ChangeSecondaryCommander(
            new DeckCommander("Partner C", ColorIdentity.Parse("R"), DeckCommanderMechanic.Partner));
        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void BasicLandMayHaveMultipleCopies()
    {
        var deck = CreateDeck("W");
        deck.AddCard("Mountain", "printing-mountain", DeckZone.Mainboard, oracleCard: new OracleCard("Mountain", "Basic Land — Mountain", ColorIdentity.Parse("C")));
        deck.IncrementCard("printing-mountain");

        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasCopyLimitViolation);
    }

    [Fact]
    public void NonBasicCardWithMultipleCopiesIsMarkedInvalid()
    {
        var deck = CreateDeck("W");
        deck.AddCard("Sol Ring", "printing-sol-ring", DeckZone.Mainboard, oracleCard: new OracleCard("Sol Ring", "Artifact", ColorIdentity.Parse("C")));
        deck.IncrementCard("printing-sol-ring");

        new DeckValidationService().Validate(deck);

        Assert.True(Assert.Single(deck.Entries).HasCopyLimitViolation);
    }

    [Theory]
    [InlineData("Seven Dwarves", 7)]
    [InlineData("Hare Apparent", 99)]
    [InlineData("Nazgul", 9)]
    [InlineData("Shadowborn Apostle", 99)]
    [InlineData("Rat Colony", 99)]
    [InlineData("Persistent Petitioners", 99)]
    [InlineData("Cid, Timeless Artificer", 99)]
    [InlineData("Relentless Rats", 99)]
    [InlineData("Dragon's Approach", 99)]
    [InlineData("Sphinx's Approach", 99)]
    [InlineData("Templar Knight", 99)]
    [InlineData("Tempest Hawk", 99)]
    [InlineData("Slime Against Humanity", 99)]
    public void NamedCopyLimitExceptionIsAllowed(string cardName, int quantity)
    {
        var deck = CreateDeck("W");
        deck.AddCard(cardName, "printing-1", DeckZone.Mainboard, oracleCard: new OracleCard(cardName, "Instant", ColorIdentity.Parse("C")));
        for (var copy = 1; copy < quantity; copy++)
        {
            deck.IncrementCard("printing-1");
        }

        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasCopyLimitViolation);
    }

    [Theory]
    [InlineData("Seven Dwarves", 8)]
    [InlineData("Nazgul", 10)]
    [InlineData("Nazgûl", 10)]
    public void CappedCopyLimitExceptionIsMarkedInvalidAboveItsLimit(
        string cardName,
        int quantity)
    {
        var deck = CreateDeck("W");
        deck.AddCard(cardName, "printing-1", DeckZone.Mainboard);
        for (var copy = 1; copy < quantity; copy++)
        {
            deck.IncrementCard("printing-1");
        }

        new DeckValidationService().Validate(deck);

        Assert.True(Assert.Single(deck.Entries).HasCopyLimitViolation);
    }

    [Fact]
    public void SameOracleAcrossDifferentPrintingsUsesCombinedQuantity()
    {
        var deck = CreateDeck("W");
        deck.AddCard("Sol Ring", "printing-1", DeckZone.Mainboard);
        deck.AddCard("Sol Ring", "printing-2", DeckZone.Sideboard);

        new DeckValidationService().Validate(deck);

        Assert.All(
            deck.Entries,
            entry => Assert.True(entry.HasCopyLimitViolation));
    }

    [Fact]
    public void BasicSnowLandAllowsMultipleCopies()
    {
        var deck = CreateDeck("W");
        deck.AddCard("Snow-Covered Plains", "printing-1", DeckZone.Mainboard, oracleCard: new OracleCard("Snow-Covered Plains", "Basic Snow Land — Plains", ColorIdentity.Parse("C")));
        deck.IncrementCard("printing-1");

        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasCopyLimitViolation);
    }

    [Fact]
    public void ReaddingExistingPrintingMergesTags()
    {
        var deck = CreateDeck("W");
        deck.AddCard("Sol Ring", "printing-1", DeckZone.Mainboard);
        deck.AddCard("Sol Ring", "printing-1", DeckZone.Mainboard, ["#Ramp"]);

        var entry = Assert.Single(deck.Entries);
        Assert.Equal(["#ramp"], entry.Tags);
        Assert.Equal(2, entry.Quantity);
    }

    private static Deck CreateDeck(string identity)
    {
        return Deck.Create(
            new DeckCommander(
                "Test Commander",
                ColorIdentity.Parse(identity),
                DeckCommanderMechanic.Primary));
    }
}
