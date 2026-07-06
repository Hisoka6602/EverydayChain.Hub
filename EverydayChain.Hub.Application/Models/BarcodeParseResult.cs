using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BarcodeParseResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BarcodeType BarcodeType { get; set; } = BarcodeType.Unknown;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string NormalizedBarcode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TargetChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BarcodeParseFailureReason FailureReason { get; set; } = BarcodeParseFailureReason.None;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FailureMessage { get; set; } = string.Empty;
}

