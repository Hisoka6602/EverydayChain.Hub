namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// Single box-tracking row assembled from scan logs and the related business task.
/// </summary>
public sealed class BoxTrackingItem
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
    /// Upstream order identifier carried by the business task.
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Upstream store identifier carried by the business task.
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// Upstream store display name carried by the business task.
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Upstream product code carried by the business task when available.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Upstream pick location carried by the business task when available.
    /// </summary>
    public string? PickLocation { get; set; }

    /// <summary>
    /// Scanner or device code recorded in the scan log.
    /// </summary>
    public string? Scanner { get; set; }

    /// <summary>
    /// Local scan timestamp recorded in the scan log.
    /// </summary>
    public DateTime ScannedAtLocal { get; set; }

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
