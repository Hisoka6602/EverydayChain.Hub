namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义 SyncCheckpoint 类型。
/// </summary>
public class SyncCheckpoint
{
    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 LastSuccessCursorLocal。
    /// </summary>
    public DateTime? LastSuccessCursorLocal { get; set; }

    /// <summary>
    /// 获取或设置 LastBatchId。
    /// </summary>
    public string? LastBatchId { get; set; }

    /// <summary>
    /// 获取或设置 LastSuccessTimeLocal。
    /// </summary>
    public DateTime? LastSuccessTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 LastError。
    /// </summary>
    public string? LastError { get; set; }
}

