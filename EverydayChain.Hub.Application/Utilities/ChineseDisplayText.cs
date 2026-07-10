using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Utilities;

/// <summary>
/// 提供对外展示用的中文文案转换。
/// </summary>
public static class ChineseDisplayText
{
    public static string ForSourceType(BusinessTaskSourceType sourceType)
    {
        return sourceType switch
        {
            BusinessTaskSourceType.Split => "拆零",
            BusinessTaskSourceType.FullCase => "整件",
            _ => "未知来源"
        };
    }

    public static string ForTaskStatus(BusinessTaskStatus status)
    {
        return status switch
        {
            BusinessTaskStatus.Created => "已创建",
            BusinessTaskStatus.Scanned => "已扫描",
            BusinessTaskStatus.Dropped => "已落格",
            BusinessTaskStatus.FeedbackPending => "待回传",
            BusinessTaskStatus.Exception => "异常",
            _ => "未知状态"
        };
    }

    public static string ForBarcodeType(BarcodeType barcodeType)
    {
        return barcodeType switch
        {
            BarcodeType.Split => "拆零条码",
            BarcodeType.FullCase => "整件条码",
            _ => "未知条码"
        };
    }

    public static string ForBarcodeParseFailureReason(BarcodeParseFailureReason failureReason)
    {
        return failureReason switch
        {
            BarcodeParseFailureReason.InvalidBarcode => "无效条码",
            BarcodeParseFailureReason.UnsupportedBarcodeType => "不支持的条码类型",
            BarcodeParseFailureReason.ParseError => "条码解析异常",
            _ => string.Empty
        };
    }

    public static string YesNo(bool value)
    {
        return value ? "是" : "否";
    }
}
