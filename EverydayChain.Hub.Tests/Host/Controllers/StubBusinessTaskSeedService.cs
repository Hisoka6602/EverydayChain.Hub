using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
internal sealed class StubBusinessTaskSeedService : IBusinessTaskSeedService
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskSeedCommand? LastCommand { get; private set; }

    public BusinessTaskSeedResult Result { get; set; } = new()
    {
        IsSuccess = true,
        Message = "模拟补数写入成功。",
        TargetTableName = "business_tasks_202604",
        RequestedCount = 2,
        FilteredEmptyCount = 0,
        DeduplicatedCount = 0,
        CandidateCount = 2,
        InsertedCount = 2,
        SkippedExistingCount = 0
    };

    public Task<BusinessTaskSeedResult> ExecuteAsync(BusinessTaskSeedCommand command, CancellationToken cancellationToken)
    {
        LastCommand = command;
        return Task.FromResult(Result);
    }
}

