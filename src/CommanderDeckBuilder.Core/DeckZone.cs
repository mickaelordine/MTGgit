namespace CommanderDeckBuilder.Core;

/// <summary>
/// Defines the zones in which a card entry can be stored.
/// </summary>
public enum DeckZone
{
    /// <summary>The zone containing the selected Commander slot.</summary>
    Commander,

    /// <summary>The zone containing the primary deck cards.</summary>
    Mainboard,

    /// <summary>The zone containing cards not used in the main deck.</summary>
    Sideboard,

    /// <summary>The zone containing cards under consideration.</summary>
    Considering
}

