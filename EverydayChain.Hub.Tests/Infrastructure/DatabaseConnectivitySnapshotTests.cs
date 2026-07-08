using EverydayChain.Hub.Application.Abstractions.Infrastructure;

namespace EverydayChain.Hub.Tests.Infrastructure;

/// <summary>
/// 定义 DatabaseConnectivitySnapshotTests 类型。
/// </summary>
public sealed class DatabaseConnectivitySnapshotTests
{
    [Fact]
    public void BuildUserMessage_ShouldJoinUnavailableDatabaseDescriptions()
    {
        var snapshot = new DatabaseConnectivitySnapshot
        {
            LocalSqlServer = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "本地 MSSQL",
                IsAvailable = false,
                Description = "本地 MSSQL 无法连接（连接超时）"
            },
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "远端 Oracle",
                IsAvailable = false,
                Description = "远端 Oracle 无法连接（监听未注册）"
            }
        };

        var message = snapshot.BuildUserMessage();

        Assert.Equal("数据库连接不可用：本地 MSSQL 无法连接（连接超时）；远端 Oracle 无法连接（监听未注册）。", message);
    }

    [Fact]
    public void BuildUserMessage_ShouldReturnFallbackMessage_WhenNoDescriptionProvided()
    {
        var snapshot = new DatabaseConnectivitySnapshot
        {
            LocalSqlServer = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "本地 MSSQL",
                IsAvailable = true,
                Description = string.Empty
            },
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "远端 Oracle",
                IsAvailable = true,
                Description = string.Empty
            }
        };

        var message = snapshot.BuildUserMessage();

        Assert.Equal("数据库连接不可用，请稍后重试。", message);
    }
}
