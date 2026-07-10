using System.Reflection;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;
using EverydayChain.Hub.Infrastructure.Sync.Readers;

namespace EverydayChain.Hub.Tests.Sync;

/// <summary>
/// 定义 OracleStatusDrivenSourceReaderTests 类型。
/// </summary>
public class OracleStatusDrivenSourceReaderTests
{
    [Fact]
    public void BuildReadSql_WhenIgnorePendingStatusValueEnabled_ShouldSkipStatusPredicateAndKeepCursorWindow()
    {
        var definition = CreateDefinition();
        var profile = new RemoteStatusConsumeProfile
        {
            StatusColumnName = "TASKPROCESS",
            PendingStatusValue = "N",
            IgnorePendingStatusValue = true,
        };

        var sql = InvokeBuildReadSql(definition, profile, hasCursorFilter: true);

        Assert.DoesNotContain("TASKPROCESS = :p_pendingStatus", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("TASKPROCESS IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("ADDTIME >= :p_windowStart AND ADDTIME <= :p_windowEnd", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildReadSql_WhenPendingStatusValueIsNull_ShouldUseIsNullPredicate()
    {
        var definition = CreateDefinition();
        var profile = new RemoteStatusConsumeProfile
        {
            StatusColumnName = "TASKPROCESS",
            PendingStatusValue = null,
            IgnorePendingStatusValue = false,
        };

        var sql = InvokeBuildReadSql(definition, profile, hasCursorFilter: true);

        Assert.Contains("TASKPROCESS IS NULL", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("TASKPROCESS = :p_pendingStatus", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildReadSql_WhenPendingStatusValueProvided_ShouldUseEqualityPredicate()
    {
        var definition = CreateDefinition();
        var profile = new RemoteStatusConsumeProfile
        {
            StatusColumnName = "TASKPROCESS",
            PendingStatusValue = "N",
            IgnorePendingStatusValue = false,
        };

        var sql = InvokeBuildReadSql(definition, profile, hasCursorFilter: false);

        Assert.Contains("TASKPROCESS = :p_pendingStatus", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("TASKPROCESS IS NULL", sql, StringComparison.Ordinal);
    }

    private static SyncTableDefinition CreateDefinition()
    {
        return new SyncTableDefinition
        {
            TableCode = "T1",
            SourceSchema = "SRC",
            SourceTable = "TAB1",
            CursorColumn = "ADDTIME",
        };
    }

    private static string InvokeBuildReadSql(SyncTableDefinition definition, RemoteStatusConsumeProfile profile, bool hasCursorFilter)
    {
        var method = typeof(OracleStatusDrivenSourceReader).GetMethod(
            "BuildReadSql",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(SyncTableDefinition), typeof(RemoteStatusConsumeProfile), typeof(bool)],
            modifiers: null);
        Assert.NotNull(method);
        var result = method!.Invoke(null, [definition, profile, hasCursorFilter]);
        Assert.IsType<string>(result);
        return (string)result;
    }
}
