using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 业务任务模拟补数仓储替身。
/// </summary>
internal sealed class StubBusinessTaskSeedRepository : IBusinessTaskSeedRepository
{
    /// <summary>
    /// 最近一次执行命令。
    /// </summary>
    public BusinessTaskSeedCommand? LastCommand { get; private set; }

    /// <summary>
    /// 返回结果工厂。
    /// </summary>
    public Func<BusinessTaskSeedCommand, BusinessTaskSeedResult> ResultFactory { get; set; } = command => new BusinessTaskSeedResult
    {
        IsSuccess = true,
        Message = "模拟补数写入成功。",
        TargetTableName = command.TargetTableName,
        InsertedCount = command.Barcodes.Count,
        SkippedExistingCount = 0
    };

    /// <inheritdoc/>
    public Task<BusinessTaskSeedResult> InsertManualSeedAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken)
    {
        LastCommand = command;
        return Task.FromResult(ResultFactory(command));
    }
}
