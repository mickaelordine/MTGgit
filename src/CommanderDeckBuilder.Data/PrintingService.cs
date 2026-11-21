using CommanderDeckBuilder.Core;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace CommanderDeckBuilder.Data;

/// <summary>
/// Reads released card printings from the local SQLite catalog.
/// </summary>
public sealed class PrintingService : IPrintingService
{
    private readonly LocalStoragePaths _paths;

    /// <summary>
    /// Creates a printing service using the configured local database.
    /// </summary>
    /// <param name="paths">The local storage paths containing the database.</param>
    public PrintingService(LocalStoragePaths paths)
    {
        _paths = paths;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CardPrinting>> GetPrintingsForCardAsync(Guid oracleId)
    {
        await using var connection = new SqliteConnection(_paths.ConnectionString);
        await connection.OpenAsync();
        var rows = await connection.QueryAsync<PrintingRow>(
            """
            SELECT
                ScryfallId,
                SetCode,
                SetName,
                CollectorNumber,
                ReleasedAt,
                ImageUriNormal,
                ImageUriArtCrop
            FROM CardPrintings
            WHERE OracleId = @OracleId
            ORDER BY ReleasedAt DESC, ScryfallId ASC
            """,
            new { OracleId = oracleId.ToString("D") });

        return rows
            .Select(row => new CardPrinting(
                Guid.Parse(row.ScryfallId),
                row.SetCode,
                row.SetName,
                row.CollectorNumber,
                ParseReleasedAt(row.ReleasedAt),
                row.ImageUriNormal,
                row.ImageUriArtCrop))
            .ToArray();
    }

    private static DateOnly? ParseReleasedAt(string? releasedAt)
    {
        if (string.IsNullOrWhiteSpace(releasedAt))
        {
            return null;
        }

        return DateOnly.TryParse(
            releasedAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : throw new FormatException(
                $"Card printing ReleasedAt value '{releasedAt}' is not a valid ISO date.");
    }

    private sealed record PrintingRow(
        string ScryfallId,
        string SetCode,
        string SetName,
        string CollectorNumber,
        string? ReleasedAt,
        string? ImageUriNormal,
        string? ImageUriArtCrop);
}
