using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using CommanderDeckBuilder.Core;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

public sealed class PrintingValidationTests
{
    [Fact]
    public async Task Valid_printing_belongs_to_the_requested_oracle()
    {
        using var fixture = new PrintingValidationStorageFixture();
        fixture.Initialize();
        var oracleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var scryfallId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        fixture.InsertPrinting(oracleId, scryfallId);
        var service = new PrintingService(fixture.Paths);

        var isValid = await service.IsPrintingForCardAsync(oracleId, scryfallId);

        Assert.True(isValid);
    }

    [Fact]
    public async Task Cross_oracle_or_unknown_printing_is_rejected()
    {
        using var fixture = new PrintingValidationStorageFixture();
        fixture.Initialize();
        var oracleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var scryfallId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        fixture.InsertPrinting(oracleId, scryfallId);
        var service = new PrintingService(fixture.Paths);

        var crossOracle = await service.IsPrintingForCardAsync(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            scryfallId);
        var unknownPrinting = await service.IsPrintingForCardAsync(
            oracleId,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        Assert.False(crossOracle);
        Assert.False(unknownPrinting);
    }

    [Fact]
    public async Task Repository_failure_is_propagated()
    {
        var paths = new LocalStoragePaths(
            Path.Combine(
                Path.GetTempPath(),
                "CommanderDeckBuilderTests",
                Guid.NewGuid().ToString("N")));
        var service = new PrintingService(paths);

        await Assert.ThrowsAsync<SqliteException>(
            () => service.IsPrintingForCardAsync(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));
    }

    private sealed class PrintingValidationStorageFixture : IDisposable
    {
        private readonly string _root = Path.Combine(
            Path.GetTempPath(),
            "CommanderDeckBuilderTests",
            Guid.NewGuid().ToString("N"));

        public PrintingValidationStorageFixture()
        {
            Paths = new LocalStoragePaths(_root);
        }

        public LocalStoragePaths Paths { get; }

        public void Initialize()
        {
            new SqliteDatabaseInitializer(Paths).Initialize();
        }

        public void InsertPrinting(Guid oracleId, Guid scryfallId)
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
            connection.Execute(
                """
                INSERT INTO CardPrintings (
                    ScryfallId, OracleId, SetCode, SetName, CollectorNumber,
                    Rarity)
                VALUES (
                    @ScryfallId, @OracleId, 'TST', 'Test Set', '1',
                    'common')
                """,
                new
                {
                    ScryfallId = scryfallId.ToString("D"),
                    OracleId = oracleId.ToString("D")
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
