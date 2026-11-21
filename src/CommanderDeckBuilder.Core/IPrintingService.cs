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
