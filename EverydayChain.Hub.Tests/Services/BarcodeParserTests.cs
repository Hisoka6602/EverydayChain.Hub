using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 条码解析服务测试。
/// </summary>
public sealed class BarcodeParserTests
{
    /// <summary>
    /// 拆零条码应解析为 Split。
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnSplit_WhenBarcodeMatchesSplitPattern()
    {
        var parser = new BarcodeParser();

        var result = parser.Parse("021103013145");

        Assert.True(result.IsValid);
        Assert.Equal(BarcodeType.Split, result.BarcodeType);
        Assert.Equal("021103013145", result.NormalizedBarcode);
        Assert.Equal("1", result.TargetChuteCode);
        Assert.Equal(BarcodeParseFailureReason.None, result.FailureReason);
    }

    /// <summary>
    /// 整件条码应解析为 FullCase。
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnFullCase_WhenBarcodeMatchesFullCasePattern()
    {
        var parser = new BarcodeParser();

        var result = parser.Parse("z46030202060001");

        Assert.True(result.IsValid);
        Assert.Equal(BarcodeType.FullCase, result.BarcodeType);
        Assert.Equal("Z46030202060001", result.NormalizedBarcode);
        Assert.Equal("4", result.TargetChuteCode);
        Assert.Equal(BarcodeParseFailureReason.None, result.FailureReason);
    }

    /// <summary>
    /// 不支持条码应返回 UnsupportedBarcodeType。
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnUnsupportedBarcodeType_WhenBarcodeCannotBeMapped()
    {
        var parser = new BarcodeParser();

        var result = parser.Parse("XYZ-001");

        Assert.False(result.IsValid);
        Assert.Equal(BarcodeType.Unknown, result.BarcodeType);
        Assert.Equal(string.Empty, result.TargetChuteCode);
        Assert.Equal(BarcodeParseFailureReason.UnsupportedBarcodeType, result.FailureReason);
    }

    /// <summary>
    /// 标识前缀后的首位字符非数字时应返回 UnsupportedBarcodeType。
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnUnsupportedBarcodeType_WhenChuteCodeIsNotDigit()
    {
        var parser = new BarcodeParser();

        var result = parser.Parse("02A1103013145");

        Assert.False(result.IsValid);
        Assert.Equal(BarcodeType.Unknown, result.BarcodeType);
        Assert.Equal(string.Empty, result.TargetChuteCode);
        Assert.Equal(BarcodeParseFailureReason.UnsupportedBarcodeType, result.FailureReason);
    }
}
