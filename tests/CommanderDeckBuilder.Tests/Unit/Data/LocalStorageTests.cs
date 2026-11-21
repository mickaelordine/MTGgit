using System.IO;
using CommanderDeckBuilder.Data;
using CommanderDeckBuilder.UI;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice2LocalStorageTests
{
    [Fact]
    public void Initialization_creates_database_and_image_cache_directories()
    {
        using var fixture = new StorageFixture();

        fixture.Initializer.Initialize();

        Assert.True(Directory.Exists(fixture.Paths.DatabaseDirectory));
        Assert.True(Directory.Exists(fixture.Paths.ImageCacheDirectory));
    }

    [Fact]
    public void Initialization_creates_the_local_database_file()
    {
        using var fixture = new StorageFixture();

        fixture.Initializer.Initialize();

        Assert.True(File.Exists(fixture.Paths.DatabaseFile));
    }

    [Fact]
    public void Initialization_creates_the_foundation_schema()
    {
        using var fixture = new StorageFixture();
        fixture.Initializer.Initialize();

        using var connection = new SqliteConnection(fixture.Paths.ConnectionString);
        connection.Open();
        var tables = connection.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name").ToArray();

        Assert.Contains("Cards", tables);
        Assert.Contains("Decks", tables);
        Assert.Contains("DeckCards", tables);
        Assert.Contains("Commits", tables);
    }

    [Fact]
    public void Initialization_can_be_run_repeatedly()
    {
        using var fixture = new StorageFixture();

        fixture.Initializer.Initialize();
        var firstWriteTime = File.GetLastWriteTimeUtc(fixture.Paths.DatabaseFile);

        fixture.Initializer.Initialize();

        Assert.True(File.Exists(fixture.Paths.DatabaseFile));
        Assert.True(File.GetLastWriteTimeUtc(fixture.Paths.DatabaseFile) >= firstWriteTime);
    }

    [Fact]
    public void Composition_root_registers_the_database_initializer()
    {
        using var provider = AppHost.CreateServiceProvider();

        Assert.NotNull(provider.GetRequiredService<SqliteDatabaseInitializer>());
    }

    private sealed class StorageFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "CommanderDeckBuilderTests", Guid.NewGuid().ToString("N"));

        public StorageFixture()
        {
            Paths = new LocalStoragePaths(_root);
            Initializer = new SqliteDatabaseInitializer(Paths);
        }

        public LocalStoragePaths Paths { get; }
        public SqliteDatabaseInitializer Initializer { get; }

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
