using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 同步删除仓储基础实现（基于内存目标数据对比）。
/// </summary>
public class SyncDeletionRepository(IOracleSourceReader oracleSourceReader, ISyncUpsertRepository upsertRepository) : ISyncDeletionRepository
{
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
        foreach (var row in targetRows)
        {
            ct.ThrowIfCancellationRequested();
            var businessKey = upsertRepository.BuildBusinessKey(row, request.UniqueKeys);
            if (string.IsNullOrWhiteSpace(businessKey))
            {
                continue;
            }

            if (sourceKeys.Contains(businessKey))
            {
                continue;
            }

            candidates.Add(new SyncDeletionCandidate
            {
                BusinessKey = businessKey,
                TargetSnapshot = new Dictionary<string, object?>(row),
                SourceEvidence = "窗口内源端未检索到该业务键。",
            });
        }

        return candidates;
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
