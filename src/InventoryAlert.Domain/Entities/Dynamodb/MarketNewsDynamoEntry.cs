namespace InventoryAlert.Domain.Entities.Dynamodb;

public class MarketNewsDynamoEntry
{
    /// <summary>
    /// Partition Key: CATEGORY#<category>
    /// </summary>
    public string PK { get; set; } = string.Empty;

    /// <summary>
    /// Sort Key: TS#<unix_timestamp_ms>
    /// </summary>
    public string SK { get; set; } = string.Empty;

    public long NewsId { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PublishedAt { get; set; } = string.Empty;
    public string SyncedAt { get; set; } = string.Empty;
}

