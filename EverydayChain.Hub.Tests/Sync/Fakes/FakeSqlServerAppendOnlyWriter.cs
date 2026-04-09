using EverydayChain.Hub.Infrastructure.Sync.Abstractions;

namespace EverydayChain.Hub.Tests.Sync.Fakes;

/// <summary>
/// 仅追加写入测试替身。
/// </summary>
public class FakeSqlServerAppendOnlyWriter : ISqlServerAppendOnlyWriter
{
    /// <summary>累计追加行数。</summary>
    public int TotalAppended { get; private set; }

    /// <inheritdoc/>
    public Task<int> AppendAsync(
        string tableCode,
        string targetLogicalTable,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken ct)
    {
        TotalAppended += rows.Count;
        return Task.FromResult(rows.Count);
    }
}
