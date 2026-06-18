namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// API response row for a single box-tracking record.
/// </summary>
public sealed class BoxTrackingItemResponse
{
    /// <summary>
    /// Box barcode uploaded during the scan call.
    /// </summary>
    public string BoxId { get; set; } = string.Empty;

    /// <summary>
    /// Local task code associated with the scanned box.
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// Wave code associated with the scanned box.
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// Upstream order identifier.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Upstream store identifier.
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// Upstream store display name.
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Upstream product code when provided by the source table.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Upstream pick location when provided by the source table.
    /// </summary>
    public string? PickLocation { get; set; }

    /// <summary>
    /// Scanner or device code recorded in the scan log.
    /// </summary>
    public string? Scanner { get; set; }

    /// <summary>
    /// Local scan timestamp recorded in the scan log.
    /// </summary>
    public DateTime ScannedAt { get; set; }

    /// <summary>
    /// Resolved chute code for the related business task.
    /// </summary>
    public string? Chute { get; set; }

    /// <summary>
    /// Box-tracking status derived from the scan result and task state.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Whether the scan matched an existing business task.
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// Failure reason recorded on the scan log when matching failed.
    /// </summary>
    public string? FailureReason { get; set; }
}
