using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Tests.Sync.Fakes;

/// <summary>
/// 定义 FakeOracleRemoteStatusWriter 类型。
/// </summary>
public class FakeOracleRemoteStatusWriter : IOracleRemoteStatusWriter
{
    /// <summary>
    /// 获取或设置 TotalWriteBackRows。
    /// </summary>
    public int TotalWriteBackRows { get; private set; }

    /// <summary>
    /// 获取或设置 LastBatchId。
    /// </summary>
    public string LastBatchId { get; private set; } = string.Empty;

    /// <summary>
    /// 获取或设置 ThrowOnWriteBack。
    /// </summary>
    public bool ThrowOnWriteBack { get; set; }

    /// <summary>
    /// 获取或设置 ThrowMessage。
    /// </summary>
    public string ThrowMessage { get; set; } = "模拟 Oracle 回写异常";

    /// <summary>
    /// 执行 WriteBackByRowIdAsync 方法。
    /// </summary>
    public Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        string batchId,
        IReadOnlyList<string> rowIds,
        CancellationToken ct)
    {
        // 步骤：执行 WriteBackByRowIdAsync 方法的核心处理流程。
        if (ThrowOnWriteBack) {
            throw new InvalidOperationException(ThrowMessage);
        }

        LastBatchId = batchId;
        TotalWriteBackRows += rowIds.Count;
        return Task.FromResult(rowIds.Count);
    }
}

