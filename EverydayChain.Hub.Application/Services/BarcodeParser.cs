using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using NLog;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 条码解析服务，实现拆零、整件与无效码分类。
/// </summary>
public sealed class BarcodeParser : IBarcodeParser
{
    /// <summary>
    /// 条码解析日志记录器。
    /// </summary>
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 解析条码文本并返回统一语义结果。
    /// </summary>
    /// <param name="barcodeText">待解析条码文本。</param>
    /// <returns>解析结果。</returns>
    public BarcodeParseResult Parse(string barcodeText)
    {
        try
        {
            var normalizedBarcode = barcodeText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedBarcode))
            {
                return BuildFailureResult(BarcodeParseFailureReason.InvalidBarcode, "条码不能为空。");
            }

            normalizedBarcode = normalizedBarcode.ToUpperInvariant();
            if (IsSplitBarcode(normalizedBarcode))
            {
                return BuildSuccessResult(normalizedBarcode, BarcodeType.Split);
            }

            if (IsFullCaseBarcode(normalizedBarcode))
            {
                return BuildSuccessResult(normalizedBarcode, BarcodeType.FullCase);
            }

            return BuildFailureResult(BarcodeParseFailureReason.UnsupportedBarcodeType, "条码类型不受支持。", normalizedBarcode);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "条码解析异常。");
            return BuildFailureResult(BarcodeParseFailureReason.ParseError, "条码解析异常。");
        }
    }

    /// <summary>
    /// 判断是否为拆零条码。
    /// </summary>
    /// <param name="normalizedBarcode">标准化条码。</param>
    /// <returns>是拆零条码返回 true，否则返回 false。</returns>
    private static bool IsSplitBarcode(string normalizedBarcode)
    {
        return normalizedBarcode.StartsWith("SPLIT-", StringComparison.Ordinal)
            || normalizedBarcode.StartsWith("S-", StringComparison.Ordinal)
            || normalizedBarcode.StartsWith("SP", StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断是否为整件条码。
    /// </summary>
    /// <param name="normalizedBarcode">标准化条码。</param>
    /// <returns>是整件条码返回 true，否则返回 false。</returns>
    private static bool IsFullCaseBarcode(string normalizedBarcode)
    {
        return normalizedBarcode.StartsWith("CASE-", StringComparison.Ordinal)
            || normalizedBarcode.StartsWith("F-", StringComparison.Ordinal)
            || IsNumericBarcodeWithSupportedLength(normalizedBarcode);
    }

    /// <summary>
    /// 判断是否为受支持长度的纯数字条码。
    /// </summary>
    /// <param name="normalizedBarcode">标准化条码。</param>
    /// <returns>符合长度与字符约束返回 true，否则返回 false。</returns>
    private static bool IsNumericBarcodeWithSupportedLength(string normalizedBarcode)
    {
        if (normalizedBarcode.Length is not (12 or 13 or 14 or 18))
        {
            return false;
        }

        foreach (var character in normalizedBarcode)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 构建解析成功结果。
    /// </summary>
    /// <param name="normalizedBarcode">标准化条码。</param>
    /// <param name="barcodeType">条码类型。</param>
    /// <returns>成功结果。</returns>
    private static BarcodeParseResult BuildSuccessResult(string normalizedBarcode, BarcodeType barcodeType)
    {
        return new BarcodeParseResult
        {
            IsValid = true,
            BarcodeType = barcodeType,
            NormalizedBarcode = normalizedBarcode,
            FailureReason = BarcodeParseFailureReason.None,
            FailureMessage = string.Empty
        };
    }

    /// <summary>
    /// 构建解析失败结果。
    /// </summary>
    /// <param name="failureReason">失败语义。</param>
    /// <param name="failureMessage">失败描述。</param>
    /// <param name="normalizedBarcode">标准化条码。</param>
    /// <returns>失败结果。</returns>
    private static BarcodeParseResult BuildFailureResult(BarcodeParseFailureReason failureReason, string failureMessage, string normalizedBarcode = "")
    {
        return new BarcodeParseResult
        {
            IsValid = false,
            BarcodeType = BarcodeType.Unknown,
            NormalizedBarcode = normalizedBarcode,
            FailureReason = failureReason,
            FailureMessage = failureMessage
        };
    }
}
