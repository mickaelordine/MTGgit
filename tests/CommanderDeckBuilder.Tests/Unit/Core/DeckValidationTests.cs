using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class DeckValidationTests
{
    [Fact]
    public void CardIdentityContainedByCommanderIsValid()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("Dimir Card", "printing-1", DeckZone.Mainboard);
        Assert.Single(deck.Entries).CardColorIdentity = ColorIdentity.Parse("U,B");

        var service = new DeckValidationService();
        service.Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void CardOutsideCommanderIdentityIsMarkedInvalid()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("Red Card", "printing-1", DeckZone.Mainboard);
        Assert.Single(deck.Entries).CardColorIdentity = ColorIdentity.Parse("R");

        new DeckValidationService().Validate(deck);

        Assert.True(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void ColorlessCardIsValidForColoredCommander()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("Wastes", "printing-1", DeckZone.Sideboard);
        Assert.Single(deck.Entries).CardColorIdentity = ColorIdentity.Parse("C");

        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasColorIdentityViolation);
    }

    [Fact]
    public void ChangingCommanderRevalidatesExistingCards()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("Red Card", "printing-1", DeckZone.Mainboard);
        Assert.Single(deck.Entries).CardColorIdentity = ColorIdentity.Parse("R");

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
        deck.AddCard("Black Card", "printing-1", zone);
        Assert.Single(deck.Entries).CardColorIdentity = ColorIdentity.Parse("B");

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
        deck.AddCard("Red Card", "printing-1", DeckZone.Mainboard);
        Assert.Single(deck.Entries).CardColorIdentity = ColorIdentity.Parse("R");

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
        deck.AddCard("Mountain", "printing-mountain", DeckZone.Mainboard);
        deck.IncrementCard("printing-mountain");

        Assert.Single(deck.Entries).CardTypeLine = "Basic Land — Mountain";
        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasCopyLimitViolation);
    }

    [Fact]
    public void NonBasicCardWithMultipleCopiesIsMarkedInvalid()
    {
        var deck = CreateDeck("W");
        deck.AddCard("Sol Ring", "printing-sol-ring", DeckZone.Mainboard);
        deck.IncrementCard("printing-sol-ring");

        Assert.Single(deck.Entries).CardTypeLine = "Artifact";
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
        deck.AddCard(cardName, "printing-1", DeckZone.Mainboard);
        for (var copy = 1; copy < quantity; copy++)
        {
            deck.IncrementCard("printing-1");
        }

        Assert.Single(deck.Entries).CardTypeLine = "Instant";
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
        deck.AddCard("Snow-Covered Plains", "printing-1", DeckZone.Mainboard);
        deck.IncrementCard("printing-1");

        Assert.Single(deck.Entries).CardTypeLine = "Basic Snow Land — Plains";
        new DeckValidationService().Validate(deck);

        Assert.False(Assert.Single(deck.Entries).HasCopyLimitViolation);
    }

    [Fact]
    public void ReaddingExistingPrintingUpdatesCardMetadataAndTags()
    {
        var deck = CreateDeck("W");
        deck.AddCard("Sol Ring", "printing-1", DeckZone.Mainboard);
        deck.AddCard(
            "Sol Ring",
            "printing-1",
            DeckZone.Mainboard,
            ["#Ramp"],
            "Sol Ring",
            "Artifact");

        var entry = Assert.Single(deck.Entries);
        Assert.Equal("Sol Ring", entry.CardName);
        Assert.Equal("Artifact", entry.CardTypeLine);
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
