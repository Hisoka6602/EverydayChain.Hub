namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示当前自动识别波次结果。
/// </summary>
public sealed class CurrentWaveResponse
{
    /// <summary>
    /// 表示查询或统计开始时间（本地时间）。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 表示查询或统计结束时间（本地时间）。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string? WaveCode { get; set; }

    /// <summary>
    /// 表示波次备注。
    /// </summary>
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 表示箱码或业务条码。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 表示扫描发生时间（本地时间）。
    /// </summary>
    public DateTime? ScanTimeLocal { get; set; }
}

