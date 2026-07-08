using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 BarcodeType 类型。
/// </summary>
public enum BarcodeType
{
    [Description("未知条码")]
    Unknown = 0,

    [Description("拆零条码")]
    Split = 1,

    [Description("整件条码")]
    FullCase = 2
}

