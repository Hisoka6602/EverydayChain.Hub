using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 扫描事件输入模型，承载后续匹配与执行链路所需字段。
/// </summary>
public sealed class ScanEventArgs
{
    /// <summary>
    /// 原始条码文本。
    /// </summary>
    public string RawBarcode { get; set; } = string.Empty;

    /// <summary>
    /// 标准化条码文本。
    /// </summary>
    public string NormalizedBarcode { get; set; } = string.Empty;

    /// <summary>
    /// 条码类型。
    /// </summary>
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Unknown;

    /// <summary>
    /// 设备编码。
    /// </summary>
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 扫描时间（本地时间）。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 链路追踪标识。
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 包裹长度，单位毫米。
    /// </summary>
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 包裹宽度，单位毫米。
    /// </summary>
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 包裹高度，单位毫米。
    /// </summary>
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 包裹体积，单位立方毫米。
    /// </summary>
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 包裹重量，单位克。
    /// </summary>
    public decimal? WeightGram { get; set; }
}
