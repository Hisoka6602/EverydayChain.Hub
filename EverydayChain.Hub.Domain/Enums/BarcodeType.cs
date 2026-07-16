using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 BarcodeType 类型。
/// </summary>
public enum BarcodeType
{
    /// <summary>
    /// 表示未知或无法识别的条码类型。
    /// </summary>
    [Description("未知条码")]
    Unknown = 0,

    /// <summary>
    /// 表示拆零业务条码。
    /// </summary>
    [Description("拆零条码")]
    Split = 1,

    /// <summary>
    /// 表示整件业务条码。
    /// </summary>
    [Description("整件条码")]
    FullCase = 2
}

