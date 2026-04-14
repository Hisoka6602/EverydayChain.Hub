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

        var result = parser.Parse("02-A1");

        Assert.True(result.IsValid);
        Assert.Equal(BarcodeType.Split, result.BarcodeType);
        Assert.Equal("02-A1", result.NormalizedBarcode);
        Assert.Equal("A1", result.TargetChuteCode);
        Assert.Equal(BarcodeParseFailureReason.None, result.FailureReason);
    }

    /// <summary>
    /// 整件条码应解析为 FullCase。
    /// </summary>
    [Fact]
    public void Parse_ShouldReturnFullCase_WhenBarcodeMatchesFullCasePattern()
    {
        var parser = new BarcodeParser();

        var result = parser.Parse("z-b2");

        Assert.True(result.IsValid);
        Assert.Equal(BarcodeType.FullCase, result.BarcodeType);
        Assert.Equal("Z-B2", result.NormalizedBarcode);
        Assert.Equal("B2", result.TargetChuteCode);
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
}
