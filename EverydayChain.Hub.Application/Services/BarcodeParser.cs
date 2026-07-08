using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using NLog;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义 BarcodeParser 类型。
/// </summary>
public sealed class BarcodeParser : IBarcodeParser
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

    private static bool TryExtractSplitChuteCode(string normalizedBarcode, out string targetChuteCode)
    {
        return TryExtractChuteCodeByPrefix(normalizedBarcode, "02", out targetChuteCode);
    }

    private static bool TryExtractFullCaseChuteCode(string normalizedBarcode, out string targetChuteCode)
    {
        return TryExtractChuteCodeByPrefix(normalizedBarcode, "Z", out targetChuteCode);
    }

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

        var chuteCharacter = normalizedBarcode[prefix.Length];
        if (!char.IsAsciiDigit(chuteCharacter))
        {
            return false;
        }

        targetChuteCode = chuteCharacter.ToString();
        return true;
    }

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

