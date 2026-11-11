namespace CommanderDeckBuilder.Data;

/// \brief Provides the filesystem locations used by the local application storage.
public sealed class LocalStoragePaths
{
    /// \brief Initializes paths under the current user's local application data directory.
    public LocalStoragePaths()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CommanderDeckBuilder"))
    {
    }

    /// \brief Initializes paths under a caller-provided root directory.
    /// \param rootDirectory The root directory for the application data.
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
