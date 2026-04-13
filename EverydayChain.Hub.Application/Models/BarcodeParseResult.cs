using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 条码解析结果模型。
/// </summary>
public sealed class BarcodeParseResult
{
    /// <summary>
    /// 解析后的条码类型。
    /// </summary>
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Unknown;

    /// <summary>
    /// 标准化条码值。
    /// </summary>
    public string NormalizedBarcode { get; set; } = string.Empty;

    /// <summary>
    /// 是否解析有效。
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 失败语义，成功时为 None。
    /// </summary>
    public BarcodeParseFailureReason FailureReason { get; set; } = BarcodeParseFailureReason.None;

    /// <summary>
    /// 失败描述，成功时为空。
    /// </summary>
    public string FailureMessage { get; set; } = string.Empty;
}
