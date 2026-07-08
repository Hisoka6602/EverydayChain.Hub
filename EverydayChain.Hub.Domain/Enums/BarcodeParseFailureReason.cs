using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 BarcodeParseFailureReason 类型。
/// </summary>
public enum BarcodeParseFailureReason
{
    [Description("无失败")]
    None = 0,

    [Description("无效条码")]
    InvalidBarcode = 1,

    [Description("不支持的条码类型")]
    UnsupportedBarcodeType = 2,

    [Description("解析异常")]
    ParseError = 3
}

