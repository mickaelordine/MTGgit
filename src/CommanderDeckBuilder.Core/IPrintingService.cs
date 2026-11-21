namespace CommanderDeckBuilder.Core;

/// <summary>
/// Provides card-printing lookup operations.
/// </summary>
public interface IPrintingService
{
    /// <summary>
    /// Gets every released printing for an Oracle card.
    /// </summary>
    /// <param name="oracleId">The Oracle card identifier.</param>
    /// <returns>The printings associated with the Oracle card.</returns>
    Task<IReadOnlyList<CardPrinting>> GetPrintingsForCardAsync(Guid oracleId);
    /// <summary>
    /// Checks whether a Scryfall printing belongs to an Oracle card.
    /// </summary>
    /// <param name="oracleId">The Oracle card identifier.</param>
    /// <param name="scryfallId">The Scryfall printing identifier.</param>
    /// <returns>True when the printing belongs to the Oracle card.</returns>
    Task<bool> IsPrintingForCardAsync(Guid oracleId, Guid scryfallId);
}

/// <summary>
/// Represents one released printing of an Oracle card.
/// </summary>
public sealed record CardPrinting(
    Guid ScryfallId,
    string SetCode,
    string SetName,
    string CollectorNumber,
    DateOnly? ReleasedAt,
    string? ImageUriNormal,
    string? ImageUriArtCrop);
