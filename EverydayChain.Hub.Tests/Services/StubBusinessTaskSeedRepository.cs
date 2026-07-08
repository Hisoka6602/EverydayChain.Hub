using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 StubBusinessTaskSeedRepository 类型。
/// </summary>
internal sealed class StubBusinessTaskSeedRepository : IBusinessTaskSeedRepository
{
    /// <summary>
    /// 获取或设置 LastCommand。
    /// </summary>
    public BusinessTaskSeedCommand? LastCommand { get; private set; }

    /// <summary>
    /// 获取或设置 command。
    /// </summary>
    public Func<BusinessTaskSeedCommand, BusinessTaskSeedResult> ResultFactory { get; set; } = command => new BusinessTaskSeedResult
    {
        IsSuccess = true,
        Message = "模拟补数写入成功。",
        TargetTableName = command.TargetTableName,
        InsertedCount = command.Barcodes.Count,
        SkippedExistingCount = 0
    };

    public Task<BusinessTaskSeedResult> InsertManualSeedAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken)
    {
        LastCommand = command;
        return Task.FromResult(ResultFactory(command));
    }
}

