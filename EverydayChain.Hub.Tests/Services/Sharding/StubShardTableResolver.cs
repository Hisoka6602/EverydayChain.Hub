using EverydayChain.Hub.Application.Abstractions.Persistence;

namespace EverydayChain.Hub.Tests.Services.Sharding;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class StubShardTableResolver : IShardTableResolver
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _physicalTables;

    public StubShardTableResolver(IReadOnlyDictionary<string, IReadOnlyList<string>>? physicalTables = null)
    {
        _physicalTables = physicalTables ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<string>> ListPhysicalTablesAsync(string logicalTableName, CancellationToken ct)
    {
        if (_physicalTables.TryGetValue(logicalTableName, out var tables))
        {
            return Task.FromResult(tables);
        }

        return Task.FromResult<IReadOnlyList<string>>([]);
    }

    public DateTime? TryParseShardMonth(string physicalTableName)
    {
        return null;
    }
}

