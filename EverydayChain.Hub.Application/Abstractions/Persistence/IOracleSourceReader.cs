using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IOracleSourceReader
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<SyncReadResult> ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadRowsByBusinessKeysAsync(
        OracleBusinessKeyRowReadRequest request,
        CancellationToken ct);
}

