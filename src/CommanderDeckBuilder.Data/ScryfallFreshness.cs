namespace CommanderDeckBuilder.Data;

public enum BulkSyncDecision
{
    UpToDate,
    DownloadRequired
}

public static class ScryfallFreshness
{
    public static BulkSyncDecision Decide(
        BulkDatasetMetadata remote,
        DateTimeOffset? localUpdatedAt)
    {
        return localUpdatedAt is null || remote.UpdatedAt > localUpdatedAt.Value
            ? BulkSyncDecision.DownloadRequired
            : BulkSyncDecision.UpToDate;
    }
}
