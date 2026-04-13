using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 条码解析失败语义枚举，统一扫描解析失败分类。
/// </summary>
public enum BarcodeParseFailureReason
{
    /// <summary>
    /// 无失败语义，表示解析成功。
    /// </summary>
    [Description("无失败")]
    None = 0,

    /// <summary>
    /// 条码为空或格式非法。
    /// </summary>
    [Description("无效条码")]
    InvalidBarcode = 1,

    /// <summary>
    /// 条码无法映射到受支持业务类型。
    /// </summary>
    [Description("不支持的条码类型")]
    UnsupportedBarcodeType = 2,

    /// <summary>
    /// 解析过程发生异常。
    /// </summary>
    [Description("解析异常")]
    ParseError = 3
}
