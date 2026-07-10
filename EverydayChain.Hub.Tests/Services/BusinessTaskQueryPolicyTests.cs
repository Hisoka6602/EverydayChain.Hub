using EverydayChain.Hub.Application.Queries;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 BusinessTaskQueryPolicyTests 类型。
/// </summary>
public sealed class BusinessTaskQueryPolicyTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("未分配码头", false)]
    [InlineData("3-5", false)]
    [InlineData("7", false)]
    [InlineData("8", true)]
    [InlineData(" 9 ", true)]
    public void IsRecirculatedByResolvedDockCode_ShouldHandleNonNumericAndNumericDockCodes(string? resolvedDockCode, bool expected)
    {
        var policy = new BusinessTaskQueryPolicy();

        var actual = policy.IsRecirculatedByResolvedDockCode(resolvedDockCode);

        Assert.Equal(expected, actual);
    }
}
