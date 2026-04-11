using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Tests.Sync.Fakes;

/// <summary>
/// Oracle 状态读取测试替身。
/// </summary>
public class FakeOracleStatusDrivenSourceReader : IOracleStatusDrivenSourceReader
{
    /// <summary>分页结果队列。</summary>
    public Queue<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Pages { get; } = new();

    /// <summary>请求页码记录。</summary>
    public List<int> RequestedPageNos { get; } = [];

    /// <summary>最近一次传入的同步窗口。</summary>
    public SyncWindow LastWindow { get; private set; }

    /// <inheritdoc/>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadPendingPageAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        int pageNo,
        int pageSize,
        IReadOnlySet<string> normalizedExcludedColumns,
        SyncWindow window,
        CancellationToken ct)
    {
        RequestedPageNos.Add(pageNo);
        LastWindow = window;
        if (Pages.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>([]);
        }

        return Task.FromResult(Pages.Dequeue());
    }
}
