using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 定义 BarcodeParseFailureReason 类型。
/// </summary>
public enum BarcodeParseFailureReason
{
    /// <summary>
    /// 表示条码解析没有失败。
    /// </summary>
    [Description("无失败")]
    None = 0,

    /// <summary>
    /// 表示条码内容为空或格式无效。
    /// </summary>
    [Description("无效条码")]
    InvalidBarcode = 1,

    /// <summary>
    /// 表示条码前缀不属于当前支持的业务类型。
    /// </summary>
    [Description("不支持的条码类型")]
    UnsupportedBarcodeType = 2,

    /// <summary>
    /// 表示条码解析过程出现异常。
    /// </summary>
    [Description("解析异常")]
    ParseError = 3
}

