using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice2ScryfallFreshnessTests
{
    [Fact]
    public void Requires_download_when_local_metadata_is_missing()
    {
        var remote = new BulkDatasetMetadata(
            new Uri("https://data.example.test/default-cards.json"),
            DateTimeOffset.Parse("2024-01-02T03:04:05Z"));

        var decision = ScryfallFreshness.Decide(remote, localUpdatedAt: null);

        Assert.Equal(BulkSyncDecision.DownloadRequired, decision);
    }

    [Fact]
    public void Skips_download_when_timestamps_are_equal()
    {
        var timestamp = DateTimeOffset.Parse("2024-01-02T03:04:05Z");
        var remote = new BulkDatasetMetadata(
            new Uri("https://data.example.test/default-cards.json"),
            timestamp);

        var decision = ScryfallFreshness.Decide(remote, timestamp);

        Assert.Equal(BulkSyncDecision.UpToDate, decision);
    }

    [Fact]
    public void Requires_download_when_remote_timestamp_is_newer()
    {
        var remote = new BulkDatasetMetadata(
            new Uri("https://data.example.test/default-cards.json"),
            DateTimeOffset.Parse("2024-01-03T03:04:05Z"));

        var decision = ScryfallFreshness.Decide(
            remote,
            DateTimeOffset.Parse("2024-01-02T03:04:05Z"));

        Assert.Equal(BulkSyncDecision.DownloadRequired, decision);
    }
}
