using EverydayChain.Hub.Application.Abstractions.Persistence;

namespace EverydayChain.Hub.Tests.Services.Sharding;

/// <summary>
/// 分表解析测试桩。
/// </summary>
public sealed class StubShardTableResolver : IShardTableResolver
{
    /// <summary>物理分表映射。</summary>
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _physicalTables;

    /// <summary>
    /// 初始化测试桩。
    /// </summary>
    /// <param name="physicalTables">逻辑表与物理表映射。</param>
    public StubShardTableResolver(IReadOnlyDictionary<string, IReadOnlyList<string>>? physicalTables = null)
    {
        _physicalTables = physicalTables ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListPhysicalTablesAsync(string logicalTableName, CancellationToken ct)
    {
        if (_physicalTables.TryGetValue(logicalTableName, out var tables))
        {
            return Task.FromResult(tables);
        }

        return Task.FromResult<IReadOnlyList<string>>([]);
    }

    /// <inheritdoc />
    public DateTime? TryParseShardMonth(string physicalTableName)
    {
        return null;
    }
}
