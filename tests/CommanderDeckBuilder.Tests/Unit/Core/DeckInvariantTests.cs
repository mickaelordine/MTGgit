using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class DeckInvariantTests
{
    private static readonly OracleCard Bolt = new("Lightning Bolt", "Instant", ColorIdentity.Parse("R"));
    private static readonly OracleCard Ring = new("Sol Ring", "Artifact", ColorIdentity.Parse("C"));

    /// <summary>
    /// Every public Deck mutator that can change validity. A new mutator must be added here
    /// (convention, see Slice 3 in the refactor plan). MoveCard and ChangePrinting are
    /// validity-neutral by rule today, so for them the theory only pins "flags still match".
    /// </summary>
    public static TheoryData<string, Action<Deck>> Mutators => new()
    {
        { "AddCard", deck => deck.AddCard("oracle-bolt", "printing-bolt-2", DeckZone.Mainboard, oracleCard: Bolt) },
        {
            "ReaddCardWithOracleCard",
            deck => deck.AddCard("oracle-ring", "printing-ring-1", DeckZone.Mainboard,
                oracleCard: new OracleCard("Sol Ring", "Basic Land — Wastes", ColorIdentity.Parse("C")))
        },
        { "IncrementCard", deck => deck.IncrementCard("printing-bolt") },
        { "DecrementCard", deck => deck.DecrementCard("printing-ring-2") },
        { "RemoveCard", deck => deck.RemoveCard("printing-ring-2") },
        { "MoveCard", deck => deck.MoveCard("printing-bolt", DeckZone.Sideboard) },
        { "ChangePrinting", deck => deck.ChangePrinting("printing-ring-2", "printing-ring-1") },
        {
            "ChangePrimaryCommander",
            deck => deck.ChangePrimaryCommander(
                new DeckCommander("Partner R", ColorIdentity.Parse("R"), DeckCommanderMechanic.Partner))
        },
        {
            "ChangeSecondaryCommander",
            deck => deck.ChangeSecondaryCommander(
                new DeckCommander("Partner R", ColorIdentity.Parse("R"), DeckCommanderMechanic.Partner))
        }
    };

    [Fact]
    public void EvaluateReportsViolationsForEveryEntry()
    {
        var deck = Deck.Create(
            new DeckCommander("Test Commander", ColorIdentity.Parse("U,B"), DeckCommanderMechanic.Primary));
        deck.AddCard("oracle-bolt", "printing-bolt", DeckZone.Mainboard, oracleCard: Bolt);
        deck.AddCard("oracle-ring", "printing-ring", DeckZone.Mainboard, oracleCard: Ring);
        deck.IncrementCard("printing-ring");

        var results = new DeckValidationService().Evaluate(deck);

        var bolt = Assert.Single(results, result => result.Entry.OracleId == "oracle-bolt");
        Assert.True(bolt.HasColorIdentityViolation);
        Assert.False(bolt.HasCopyLimitViolation);
        var ring = Assert.Single(results, result => result.Entry.OracleId == "oracle-ring");
        Assert.False(ring.HasColorIdentityViolation);
        Assert.True(ring.HasCopyLimitViolation);
        Assert.Equal(2, results.Count);
    }

    [Theory]
    [MemberData(nameof(Mutators))]
    public void EveryMutatorLeavesFlagsEqualToEvaluate(string mutator, Action<Deck> mutate)
    {
        _ = mutator;
        var deck = CreatePartnerDeckWithViolations();

        mutate(deck);

        AssertFlagsMatchEvaluate(deck);
    }

    [Fact]
    public void TagChangesDoNotAlterViolationFlags()
    {
        var deck = CreatePartnerDeckWithViolations();
        var before = deck.Entries
            .Select(entry => (entry.HasColorIdentityViolation, entry.HasCopyLimitViolation))
            .ToArray();

        deck.AddTag("printing-bolt", "#Burn");
        deck.RemoveTag("printing-bolt", "#Burn");

        var after = deck.Entries
            .Select(entry => (entry.HasColorIdentityViolation, entry.HasCopyLimitViolation))
            .ToArray();
        Assert.Equal(before, after);
        AssertFlagsMatchEvaluate(deck);
    }

    private static Deck CreatePartnerDeckWithViolations()
    {
        var deck = Deck.Create(
            new DeckCommander("Partner U", ColorIdentity.Parse("U"), DeckCommanderMechanic.Partner),
            new DeckCommander("Partner B", ColorIdentity.Parse("B"), DeckCommanderMechanic.Partner));
        deck.AddCard("oracle-bolt", "printing-bolt", DeckZone.Mainboard, oracleCard: Bolt);
        deck.AddCard("oracle-ring", "printing-ring-1", DeckZone.Mainboard, oracleCard: Ring);
        deck.AddCard("oracle-ring", "printing-ring-2", DeckZone.Sideboard, oracleCard: Ring);
        return deck;
    }

    private static void AssertFlagsMatchEvaluate(Deck deck)
    {
        var results = new DeckValidationService().Evaluate(deck);
        Assert.Equal(deck.Entries.Count, results.Count);
        Assert.All(results, result =>
        {
            Assert.Equal(result.HasColorIdentityViolation, result.Entry.HasColorIdentityViolation);
            Assert.Equal(result.HasCopyLimitViolation, result.Entry.HasCopyLimitViolation);
        });
    }
}
