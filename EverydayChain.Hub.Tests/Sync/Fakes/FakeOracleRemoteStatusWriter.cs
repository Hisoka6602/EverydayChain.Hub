using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Tests.Sync.Fakes;

/// <summary>
/// 定义当前类型。
/// </summary>
public class FakeOracleRemoteStatusWriter : IOracleRemoteStatusWriter
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int TotalWriteBackRows { get; private set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string LastBatchId { get; private set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool ThrowOnWriteBack { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string ThrowMessage { get; set; } = "模拟 Oracle 回写异常";

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<int> WriteBackByRowIdAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        string batchId,
        IReadOnlyList<string> rowIds,
        CancellationToken ct)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        if (ThrowOnWriteBack) {
            throw new InvalidOperationException(ThrowMessage);
        }

        LastBatchId = batchId;
        TotalWriteBackRows += rowIds.Count;
        return Task.FromResult(rowIds.Count);
    }
}

