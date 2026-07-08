using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BarcodeParseResult 类型。
/// </summary>
public sealed class BarcodeParseResult
{
    /// <summary>
    /// 获取或设置 BarcodeType。
    /// </summary>
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Unknown;

    /// <summary>
    /// 获取或设置 NormalizedBarcode。
    /// </summary>
    public string NormalizedBarcode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 TargetChuteCode。
    /// </summary>
    public string TargetChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 IsValid。
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    public BarcodeParseFailureReason FailureReason { get; set; } = BarcodeParseFailureReason.None;

    /// <summary>
    /// 获取或设置 FailureMessage。
    /// </summary>
    public string FailureMessage { get; set; } = string.Empty;
}

