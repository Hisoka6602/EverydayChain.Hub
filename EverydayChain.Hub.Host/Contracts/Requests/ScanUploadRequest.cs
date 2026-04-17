using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 扫描上传请求。
/// </summary>
public sealed class ScanUploadRequest {
    /// <summary>
    /// 单条条码文本（兼容字段）。
    /// 可填写范围：长度 1~128；可选。
    /// 空值语义：当 <see cref="Barcodes"/> 有值时可为空；当 <see cref="Barcodes"/> 为空时必须提供。
    /// </summary>
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;

    /// <summary>
    /// 多条条码文本列表（推荐字段）。
    /// 可填写范围：0~100 项，每项长度 1~128；可选。
    /// 空值语义：为空时回退读取 <see cref="Barcode"/>；有值时优先使用该字段。
    /// </summary>
    public List<string> Barcodes { get; set; } = [];

    /// <summary>
    /// 设备编码。
    /// 可填写范围：长度 1~64；必填。
    /// 空值语义：空字符串或仅空白字符均视为无效请求。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>
    /// 扫描时间（本地时间）。
    /// 可填写范围：本地时间语义；禁止 UTC 与时区偏移格式（如 +08:00）。
    /// 空值语义：该字段为值类型，未传时通常绑定为 <see cref="DateTime.MinValue"/>，当前规范化逻辑会替换为服务器本地当前时间。
    /// </summary>
    public DateTime ScanTimeLocal { get; set; }

    /// <summary>
    /// 链路追踪标识。
    /// 可填写范围：长度 0~64；可选。
    /// 空值语义：为空表示不携带链路追踪标识。
    /// </summary>
    [MaxLength(64)]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 包裹长度，单位毫米。
    /// 可填写范围：大于等于 0；可选。
    /// 空值语义：为空表示长度未知。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 包裹宽度，单位毫米。
    /// 可填写范围：大于等于 0；可选。
    /// 空值语义：为空表示宽度未知。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 包裹高度，单位毫米。
    /// 可填写范围：大于等于 0；可选。
    /// 空值语义：为空表示高度未知。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 包裹体积，单位立方毫米。
    /// 可填写范围：大于等于 0；可选。
    /// 空值语义：为空表示体积未知。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 包裹重量，单位克。
    /// 可填写范围：大于等于 0；可选。
    /// 空值语义：为空表示重量未知。
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? WeightGram { get; set; }
}
