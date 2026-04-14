using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using NLog;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 条码解析服务，实现拆零、整件、目标格口提取与无效码分类。
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
            if (TryExtractSplitChuteCode(normalizedBarcode, out var splitChuteCode))
            {
                return BuildSuccessResult(normalizedBarcode, BarcodeType.Split, splitChuteCode);
            }

            if (TryExtractFullCaseChuteCode(normalizedBarcode, out var fullCaseChuteCode))
            {
                return BuildSuccessResult(normalizedBarcode, BarcodeType.FullCase, fullCaseChuteCode);
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
    /// 按“02-格口号”规则提取拆零目标格口。
    /// </summary>
    /// <param name="normalizedBarcode">标准化条码。</param>
    /// <param name="targetChuteCode">提取出的目标格口编码。</param>
    /// <returns>提取成功返回 true，否则返回 false。</returns>
    private static bool TryExtractSplitChuteCode(string normalizedBarcode, out string targetChuteCode)
    {
        return TryExtractChuteCodeByPrefix(normalizedBarcode, "02-", out targetChuteCode);
    }

    /// <summary>
    /// 按“Z-格口号”规则提取整件目标格口。
    /// </summary>
    /// <param name="normalizedBarcode">标准化条码。</param>
    /// <param name="targetChuteCode">提取出的目标格口编码。</param>
    /// <returns>提取成功返回 true，否则返回 false。</returns>
    private static bool TryExtractFullCaseChuteCode(string normalizedBarcode, out string targetChuteCode)
    {
        return TryExtractChuteCodeByPrefix(normalizedBarcode, "Z-", out targetChuteCode);
    }

    /// <summary>
    /// 按固定前缀提取目标格口编码。
    /// </summary>
    /// <param name="normalizedBarcode">标准化条码。</param>
    /// <param name="prefix">条码固定前缀。</param>
    /// <param name="targetChuteCode">提取出的目标格口编码。</param>
    /// <returns>提取成功返回 true，否则返回 false。</returns>
    private static bool TryExtractChuteCodeByPrefix(string normalizedBarcode, string prefix, out string targetChuteCode)
    {
        targetChuteCode = string.Empty;
        if (!normalizedBarcode.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (normalizedBarcode.Length <= prefix.Length)
        {
            return false;
        }

        targetChuteCode = normalizedBarcode[prefix.Length..];
        if (string.IsNullOrEmpty(targetChuteCode))
        {
            return false;
        }

        foreach (var character in targetChuteCode)
        {
            if (char.IsWhiteSpace(character))
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
    /// <param name="targetChuteCode">目标格口编码。</param>
    /// <returns>成功结果。</returns>
    private static BarcodeParseResult BuildSuccessResult(string normalizedBarcode, BarcodeType barcodeType, string targetChuteCode)
    {
        return new BarcodeParseResult
        {
            IsValid = true,
            BarcodeType = barcodeType,
            NormalizedBarcode = normalizedBarcode,
            TargetChuteCode = targetChuteCode,
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
            TargetChuteCode = string.Empty,
            FailureReason = failureReason,
            FailureMessage = failureMessage
        };
    }
}
