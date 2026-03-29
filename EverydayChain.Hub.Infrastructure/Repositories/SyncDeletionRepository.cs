using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步删除仓储基础实现（基于内存目标数据对比）。
/// </summary>
public class SyncDeletionRepository(IOracleSourceReader oracleSourceReader, ISyncUpsertRepository upsertRepository) : ISyncDeletionRepository
{
    /// <summary>源端缺失证据描述。</summary>
    private const string MissingSourceEvidenceMessage = "窗口内源端未检索到该业务键。";
    /// <summary>删除差异比对默认分段大小。</summary>
    private const int DefaultCompareSegmentSize = 20000;
    /// <summary>删除差异比对默认并行度。</summary>
    private const int DefaultCompareMaxParallelism = 1;
    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncDeletionCandidate>> DetectDeletedKeysAsync(SyncDeletionDetectRequest request, CancellationToken ct)
    {
        var sourceKeys = await oracleSourceReader.ReadByKeysAsync(new SyncKeyReadRequest
        {
            TableCode = request.TableCode,
            CursorColumn = request.CursorColumn,
            Window = request.Window,
            UniqueKeys = request.UniqueKeys,
        }, ct);

        var targetRows = await upsertRepository.ListTargetRowsAsync(request.TableCode, ct);
        var candidates = new List<SyncDeletionCandidate>();
        var segmentSize = request.CompareSegmentSize > 0 ? request.CompareSegmentSize : DefaultCompareSegmentSize;
        var maxParallelism = request.CompareMaxParallelism > 0 ? request.CompareMaxParallelism : DefaultCompareMaxParallelism;
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = maxParallelism,
        };

        var candidateBag = new System.Collections.Concurrent.ConcurrentBag<SyncDeletionCandidate>();
        var segments = targetRows
            .Select((row, index) => new { row, index })
            .GroupBy(item => item.index / segmentSize)
            .Select(group => group.Select(item => item.row).ToList())
            .ToList();
        await Parallel.ForEachAsync(segments, parallelOptions, (segmentRows, token) =>
        {
            foreach (var row in segmentRows)
            {
                token.ThrowIfCancellationRequested();

                if (!IsRowWithinWindow(row, request.CursorColumn, request.Window))
                {
                    continue;
                }

                var businessKey = upsertRepository.BuildBusinessKey(row, request.UniqueKeys);
                if (string.IsNullOrWhiteSpace(businessKey) || sourceKeys.Contains(businessKey))
                {
                    continue;
                }

                candidateBag.Add(new SyncDeletionCandidate
                {
                    BusinessKey = businessKey,
                    TargetSnapshot = new Dictionary<string, object?>(row),
                    SourceEvidence = MissingSourceEvidenceMessage,
                });
            }
            return ValueTask.CompletedTask;
        });

        candidates.AddRange(candidateBag.OrderBy(x => x.BusinessKey, StringComparer.OrdinalIgnoreCase));

        return candidates;
    }

    /// <summary>
    /// 判断目标数据行是否在同步窗口内。
    /// </summary>
    /// <param name="row">目标数据行。</param>
    /// <param name="cursorColumn">游标列名。</param>
    /// <param name="window">同步窗口。</param>
    /// <returns>在窗口内返回 <c>true</c>。</returns>
    private static bool IsRowWithinWindow(IReadOnlyDictionary<string, object?> row, string cursorColumn, SyncWindow window)
    {
        if (!row.TryGetValue(cursorColumn, out var cursorValue) || cursorValue is not DateTime cursorLocal)
        {
            return false;
        }

        if (cursorLocal.Kind == DateTimeKind.Unspecified)
        {
            cursorLocal = DateTime.SpecifyKind(cursorLocal, DateTimeKind.Local);
        }

        if (cursorLocal.Kind != DateTimeKind.Local)
        {
            return false;
        }

        return cursorLocal > window.WindowStartLocal && cursorLocal <= window.WindowEndLocal;
    }

    /// <inheritdoc/>
    public Task<int> ApplyDeletionAsync(SyncDeletionApplyRequest request, CancellationToken ct)
    {
        if (request.BusinessKeys.Count == 0)
        {
            return Task.FromResult(0);
        }

        if (request.DeletionPolicy == DeletionPolicy.Disabled)
        {
            return Task.FromResult(0);
        }

        if (request.DryRun)
        {
            return Task.FromResult(0);
        }

        return upsertRepository.DeleteByBusinessKeysAsync(request.TableCode, request.BusinessKeys, request.DeletionPolicy, ct);
    }
}
