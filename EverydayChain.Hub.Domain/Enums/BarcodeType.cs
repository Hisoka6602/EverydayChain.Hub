using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 条码类型枚举，描述扫描输入在业务链路中的分类结果。
/// </summary>
public enum BarcodeType
{
    /// <summary>
    /// 无法识别的条码类型。
    /// </summary>
    [Description("未知条码")]
    Unknown = 0,

    /// <summary>
    /// 拆零条码类型。
    /// </summary>
    [Description("拆零条码")]
    Split = 1,

    /// <summary>
    /// 整件条码类型。
    /// </summary>
    [Description("整件条码")]
    FullCase = 2
}
