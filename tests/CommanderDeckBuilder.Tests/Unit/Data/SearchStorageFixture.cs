using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

internal sealed class SearchStorageFixture : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "CommanderDeckBuilderTests",
        Guid.NewGuid().ToString("N"));

    public SearchStorageFixture()
    {
        Paths = new LocalStoragePaths(_root);
    }

    public LocalStoragePaths Paths { get; }

    public void Initialize()
    {
        new SqliteDatabaseInitializer(Paths).Initialize();
    }

    public void InsertCard(
        string oracleId,
        string name,
        string normalizedName,
        string colorIdentity,
        bool isPossibleCommander)
    {
        using var connection = new SqliteConnection(Paths.ConnectionString);
        connection.Open();
        connection.Execute(
            """
            INSERT INTO OracleCards (
                OracleId, Name, NormalizedName, Cmc, TypeLine,
                ColorIdentity, IsPossibleCommander)
            VALUES (
                @OracleId, @Name, @NormalizedName, 0, 'Test',
                @ColorIdentity, @IsPossibleCommander)
            """,
            new
            {
                OracleId = oracleId,
                Name = name,
                NormalizedName = normalizedName,
                ColorIdentity = colorIdentity,
                IsPossibleCommander = isPossibleCommander
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
