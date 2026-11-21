using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using CommanderDeckBuilder.Core;
using CommanderDeckBuilder.Data;
using CommanderDeckBuilder.UI;

namespace CommanderDeckBuilder.Tests;

public sealed class PrintingServiceTests
{
    [Fact]
    public async Task Returns_all_printing_fields_for_a_valid_oracle()
    {
        using var fixture = new PrintingStorageFixture();
        fixture.Initialize();
        var oracleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var scryfallId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        fixture.InsertOracle(oracleId);
        fixture.InsertPrinting(
            oracleId,
            scryfallId,
            "ONE",
            "One Set",
            "101",
            "2024-01-01",
            "https://example.test/card-normal.jpg",
            "https://example.test/card-art.jpg");
        var service = new PrintingService(fixture.Paths);

        var printings = await service.GetPrintingsForCardAsync(oracleId);

        var printing = Assert.Single(printings);
        Assert.Equal(scryfallId, printing.ScryfallId);
        Assert.Equal("ONE", printing.SetCode);
        Assert.Equal("One Set", printing.SetName);
        Assert.Equal("101", printing.CollectorNumber);
        Assert.Equal(new DateOnly(2024, 1, 1), printing.ReleasedAt);
        Assert.Equal("https://example.test/card-normal.jpg", printing.ImageUriNormal);
        Assert.Equal("https://example.test/card-art.jpg", printing.ImageUriArtCrop);
    }

    [Fact]
    public async Task Orders_printings_by_release_date_descending()
    {
        using var fixture = new PrintingStorageFixture();
        fixture.Initialize();
        var oracleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        fixture.InsertOracle(oracleId);
        fixture.InsertPrinting(
            oracleId,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "NEW",
            "New Set",
            "1",
            "2024-12-01",
            null,
            null);
        fixture.InsertPrinting(
            oracleId,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "OLD",
            "Old Set",
            "2",
            "2020-01-01",
            null,
            null);
        var service = new PrintingService(fixture.Paths);

        var printings = await service.GetPrintingsForCardAsync(oracleId);

        Assert.Equal(["NEW", "OLD"], printings.Select(printing => printing.SetCode).ToArray());
    }

    [Fact]
    public async Task Unknown_oracle_returns_empty_list()
    {
        using var fixture = new PrintingStorageFixture();
        fixture.Initialize();
        var service = new PrintingService(fixture.Paths);

        var printings = await service.GetPrintingsForCardAsync(
            Guid.Parse("99999999-9999-9999-9999-999999999999"));

        Assert.Empty(printings);
    }

    [Fact]
    public async Task Null_release_dates_follow_dated_printings_with_stable_ties()
    {
        using var fixture = new PrintingStorageFixture();
        fixture.Initialize();
        var oracleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        fixture.InsertOracle(oracleId);
        fixture.InsertPrinting(
            oracleId,
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "D",
            "Dated Set",
            "1",
            "2024-01-01",
            null,
            null);
        fixture.InsertPrinting(
            oracleId,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "B",
            "Null Set B",
            "2",
            null,
            null,
            null);
        fixture.InsertPrinting(
            oracleId,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "A",
            "Null Set A",
            "3",
            null,
            null,
            null);
        var service = new PrintingService(fixture.Paths);

        var printings = await service.GetPrintingsForCardAsync(oracleId);

        Assert.Equal(["D", "A", "B"], printings.Select(printing => printing.SetCode).ToArray());
        Assert.Null(printings[1].ReleasedAt);
    }

    [Fact]
    public async Task Canonical_guid_maps_to_the_string_oracle_id()
    {
        using var fixture = new PrintingStorageFixture();
        fixture.Initialize();
        var oracleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        fixture.InsertOracle(oracleId);
        fixture.InsertPrinting(
            oracleId,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "ONE",
            "One Set",
            "1",
            null,
            null,
            null);
        var service = new PrintingService(fixture.Paths);

        var printings = await service.GetPrintingsForCardAsync(oracleId);

        Assert.Single(printings);
    }

    [Fact]
    public async Task Queries_card_printings_through_sqlite()
    {
        using var fixture = new PrintingStorageFixture();
        fixture.Initialize();
        var oracleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        fixture.InsertOracle(oracleId);
        fixture.InsertPrinting(
            oracleId,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "SQL",
            "SQLite Set",
            "42",
            "2023-06-15",
            null,
            null);
        var service = new PrintingService(fixture.Paths);

        var printings = await service.GetPrintingsForCardAsync(oracleId);

        Assert.Single(printings);
    }

    [Fact]
    public void Service_is_registered_in_dependency_injection()
    {
        using var provider = AppHost.CreateServiceProvider();

        var service = provider.GetRequiredService<IPrintingService>();

        Assert.IsType<PrintingService>(service);
    }

    [Fact]
    public async Task Repository_failures_are_propagated()
    {
        var paths = new LocalStoragePaths(
            Path.Combine(
                Path.GetTempPath(),
                "CommanderDeckBuilderTests",
                Guid.NewGuid().ToString("N")));
        var service = new PrintingService(paths);

        await Assert.ThrowsAsync<SqliteException>(
            () => service.GetPrintingsForCardAsync(
                Guid.Parse("11111111-1111-1111-1111-111111111111")));
    }

    private sealed class PrintingStorageFixture : IDisposable
    {
        private readonly string _root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));

        public PrintingStorageFixture()
        {
            Paths = new LocalStoragePaths(_root);
        }

        public LocalStoragePaths Paths { get; }

        public void Initialize()
        {
            new SqliteDatabaseInitializer(Paths).Initialize();
        }

        public void InsertOracle(Guid oracleId)
        {
            using var connection = new SqliteConnection(Paths.ConnectionString);
            connection.Open();
            connection.Execute(
                """
                INSERT INTO OracleCards (
                    OracleId, Name, NormalizedName, Cmc, TypeLine,
                    ColorIdentity, IsPossibleCommander)
                VALUES (
                    @OracleId, 'Test Card', 'test card', 0, 'Test',
                    '', 0)
                """,
                new { OracleId = oracleId.ToString("D") });
        }

        public void InsertPrinting(
            Guid oracleId,
            Guid scryfallId,
            string setCode,
            string setName,
            string collectorNumber,
            string? releasedAt,
            string? imageUriNormal,
            string? imageUriArtCrop)
        {
            using var connection = new SqliteConnection(Paths.ConnectionString);
            connection.Open();
            connection.Execute(
                """
                INSERT INTO CardPrintings (
                    ScryfallId, OracleId, SetCode, SetName, CollectorNumber,
                    Rarity, ReleasedAt, ImageUriNormal, ImageUriArtCrop)
                VALUES (
                    @ScryfallId, @OracleId, @SetCode, @SetName, @CollectorNumber,
                    'common', @ReleasedAt, @ImageUriNormal, @ImageUriArtCrop)
                """,
                new
                {
                    ScryfallId = scryfallId.ToString("D"),
                    OracleId = oracleId.ToString("D"),
                    SetCode = setCode,
                    SetName = setName,
                    CollectorNumber = collectorNumber,
                    ReleasedAt = releasedAt,
                    ImageUriNormal = imageUriNormal,
                    ImageUriArtCrop = imageUriArtCrop
                });
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
