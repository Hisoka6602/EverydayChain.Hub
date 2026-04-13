using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 业务任务仓储 EF Core 实现，操作 SQL Server 中的 <c>business_tasks</c> 固定表（非分片）。
/// </summary>
public class BusinessTaskRepository : IBusinessTaskRepository
{
    /// <summary>
    /// DbContext 工厂，每次操作均创建独立上下文以保证线程安全。
    /// </summary>
    private readonly IDbContextFactory<HubDbContext> _contextFactory;

    /// <summary>
    /// 初始化业务任务仓储。
    /// </summary>
    /// <param name="contextFactory">HubDbContext 工厂。</param>
    public BusinessTaskRepository(IDbContextFactory<HubDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async Task<BusinessTaskEntity?> FindByBarcodeAsync(string barcode, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.BusinessTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Barcode == barcode, ct);
    }

    /// <inheritdoc/>
    public async Task<BusinessTaskEntity?> FindByTaskCodeAsync(string taskCode, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.BusinessTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TaskCode == taskCode, ct);
    }

    /// <inheritdoc/>
    public async Task<BusinessTaskEntity?> FindByIdAsync(long id, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.BusinessTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        db.BusinessTasks.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(BusinessTaskEntity entity, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        db.BusinessTasks.Update(entity);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindPendingFeedbackAsync(int maxCount, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.BusinessTasks
            .AsNoTracking()
            .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Pending)
            .OrderBy(x => x.CreatedTimeLocal)
            .Take(maxCount)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindFailedFeedbackAsync(int maxCount, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.BusinessTasks
            .AsNoTracking()
            .Where(x => x.FeedbackStatus == BusinessTaskFeedbackStatus.Failed)
            .OrderBy(x => x.CreatedTimeLocal)
            .Take(maxCount)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<int> BulkMarkExceptionByWaveCodeAsync(
        string waveCode,
        BusinessTaskStatus targetStatus,
        string failureReasonPrefix,
        DateTime updatedTimeLocal,
        CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.BusinessTasks
            .Where(x => x.WaveCode == waveCode
                && x.Status != BusinessTaskStatus.Dropped
                && x.Status != BusinessTaskStatus.Exception)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, targetStatus)
                .SetProperty(x => x.FailureReason, failureReasonPrefix)
                .SetProperty(x => x.UpdatedTimeLocal, updatedTimeLocal),
                ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindByWaveCodeAsync(string waveCode, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.BusinessTasks
            .AsNoTracking()
            .Where(x => x.WaveCode == waveCode)
            .OrderBy(x => x.CreatedTimeLocal)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessTaskEntity>> FindActiveByBarcodeAsync(string barcode, CancellationToken ct)
    {
        using var scope = TableSuffixScope.Use(string.Empty);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        return await db.BusinessTasks
            .AsNoTracking()
            .Where(x => x.Barcode == barcode
                && x.Status != Domain.Enums.BusinessTaskStatus.Dropped
                && x.Status != Domain.Enums.BusinessTaskStatus.Exception)
            .OrderBy(x => x.CreatedTimeLocal)
            .ToListAsync(ct);
    }
}
