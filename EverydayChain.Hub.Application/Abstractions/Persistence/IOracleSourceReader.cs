using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Persistence;

/// <summary>
/// Oracle 源端读取器接口。
/// </summary>
public interface IOracleSourceReader
{
    /// <summary>
    /// 按窗口分页读取增量数据。
    /// </summary>
    /// <param name="request">读取请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>读取结果。</returns>
    Task<SyncReadResult> ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct);

    /// <summary>
    /// 按窗口读取源端业务键集合。
    /// </summary>
    /// <param name="request">业务键读取请求。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>业务键集合。</returns>
    Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct);
}
