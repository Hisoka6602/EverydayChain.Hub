using EverydayChain.Hub.Application.Abstractions.Sync;
using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Domain.Sync.Models;

namespace EverydayChain.Hub.Tests.Sync.Fakes;

/// <summary>
/// 定义当前类型。
/// </summary>
public class FakeOracleStatusDrivenSourceReader : IOracleStatusDrivenSourceReader
{
    public Queue<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Pages { get; } = new();

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public List<int> RequestedPageNos { get; } = [];

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public SyncWindow LastWindow { get; private set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public RemoteStatusConsumeProfile? LastProfile { get; private set; }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadPendingPageAsync(
        SyncTableDefinition definition,
        RemoteStatusConsumeProfile profile,
        int pageNo,
        int pageSize,
        IReadOnlySet<string> normalizedExcludedColumns,
        SyncWindow window,
        CancellationToken ct)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        RequestedPageNos.Add(pageNo);
        LastWindow = window;
        LastProfile = profile;
        if (Pages.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>([]);
        }

        return Task.FromResult(Pages.Dequeue());
    }
}

