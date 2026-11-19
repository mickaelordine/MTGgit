namespace CommanderDeckBuilder.Core;

/// <summary>
/// Defines the pairing mechanic used by a selected Commander.
/// </summary>
public enum DeckCommanderMechanic
{
    /// <summary>A standard single Commander slot.</summary>
    Primary,

    /// <summary>A Commander selected through the Partner mechanic.</summary>
    Partner,

    /// <summary>A Commander paired with a Background.</summary>
    Background,

    /// <summary>A Commander paired through the Doctor's Companion mechanic.</summary>
    DoctorsCompanion
}
