namespace CommanderDeckBuilder.Data;

/// \brief Describes whether the local bulk dataset must be refreshed.
public enum BulkSyncDecision
{
    UpToDate,
    DownloadRequired
}

/// \brief Compares remote and local bulk dataset timestamps.
public static class ScryfallFreshness
{
    /// \brief Determines whether the remote dataset must be downloaded.
    /// \param remote The remote dataset metadata.
    /// \param localUpdatedAt The local dataset timestamp, or null when absent.
    /// \return DownloadRequired when local data is absent or older; otherwise UpToDate.
    public static BulkSyncDecision Decide(
        BulkDatasetMetadata remote,
        DateTimeOffset? localUpdatedAt)
    {
        return localUpdatedAt is null || remote.UpdatedAt > localUpdatedAt.Value
            ? BulkSyncDecision.DownloadRequired
            : BulkSyncDecision.UpToDate;
    }
}
