namespace CommanderDeckBuilder.Data;

public sealed class LocalStoragePaths
{
    public LocalStoragePaths()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CommanderDeckBuilder"))
    {
    }

    public LocalStoragePaths(string rootDirectory)
    {
        RootDirectory = rootDirectory;
    }

    public string RootDirectory { get; }
    public string DatabaseDirectory => Path.Combine(RootDirectory, "Database");
    public string ImageCacheDirectory => Path.Combine(RootDirectory, "ImageCache");
    public string DatabaseFile => Path.Combine(DatabaseDirectory, "commander-deck-builder.db");
    public string ConnectionString => $"Data Source={DatabaseFile}";
}
