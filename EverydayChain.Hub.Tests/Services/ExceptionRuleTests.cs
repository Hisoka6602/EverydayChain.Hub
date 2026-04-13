using EverydayChain.Hub.Application.WaveCleanup.Services;
using EverydayChain.Hub.Application.MultiLabel.Services;
using EverydayChain.Hub.Application.Recirculation.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 异常规则服务单元测试，覆盖波次清理、多标签决策与回流规则的主要路径。
/// </summary>
public sealed class ExceptionRuleTests
{
    #region 辅助方法

    /// <summary>
    /// 构建波次清理服务，使用内存仓储替身。
    /// </summary>
    private static (WaveCleanupService Service, InMemoryBusinessTaskRepository Repository) CreateWaveCleanupService(
        bool enabled = true, bool waveEnabled = true, bool dryRun = false)
    {
        var repo = new InMemoryBusinessTaskRepository();
        var options = new ExceptionRuleOptions
        {
            Enabled = enabled,
            DryRun = dryRun,
            WaveCleanup = new WaveCleanupRuleOptions { Enabled = waveEnabled }
        };
        var service = new WaveCleanupService(repo, options, NullLogger<WaveCleanupService>.Instance);
        return (service, repo);
    }

    /// <summary>
    /// 构建多标签决策服务，使用内存仓储替身。
    /// </summary>
    private static (MultiLabelDecisionService Service, InMemoryBusinessTaskRepository Repository) CreateMultiLabelService(
        bool enabled = true, bool multiLabelEnabled = true, string strategy = "MarkException", bool dryRun = false)
    {
        var repo = new InMemoryBusinessTaskRepository();
        var options = new ExceptionRuleOptions
        {
            Enabled = enabled,
            DryRun = dryRun,
            MultiLabel = new MultiLabelRuleOptions { Enabled = multiLabelEnabled, Strategy = strategy }
        };
        var service = new MultiLabelDecisionService(repo, options, NullLogger<MultiLabelDecisionService>.Instance);
        return (service, repo);
    }

    /// <summary>
    /// 构建回流规则服务，使用内存仓储替身。
    /// </summary>
    private static (RecirculationService Service, InMemoryBusinessTaskRepository Repository) CreateRecirculationService(
        bool enabled = true, bool recirculationEnabled = true, int maxRetries = 3, bool dryRun = false)
    {
        var repo = new InMemoryBusinessTaskRepository();
        var options = new ExceptionRuleOptions
        {
            Enabled = enabled,
            DryRun = dryRun,
            Recirculation = new RecirculationRuleOptions { Enabled = recirculationEnabled, MaxScanRetries = maxRetries }
        };
        var service = new RecirculationService(repo, options, NullLogger<RecirculationService>.Instance);
        return (service, repo);
    }

    /// <summary>
    /// 构建一个测试用业务任务实体。
    /// </summary>
    private static BusinessTaskEntity BuildTask(
        string taskCode,
        BusinessTaskStatus status = BusinessTaskStatus.Created,
        string? barcode = null,
        string? waveCode = null,
        int scanRetryCount = 0)
    {
        return new BusinessTaskEntity
        {
            TaskCode = taskCode,
            SourceTableCode = "TEST",
            BusinessKey = taskCode,
            Status = status,
            Barcode = barcode,
            WaveCode = waveCode,
            ScanRetryCount = scanRetryCount,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        };
    }

    #endregion

    #region 波次清理测试

    /// <summary>
    /// 规则总开关关闭时，波次清理应返回跳过结论。
    /// </summary>
    [Fact]
    public async Task WaveCleanup_ShouldSkip_WhenDisabled()
    {
        var (service, _) = CreateWaveCleanupService(enabled: false);

        var result = await service.CleanByWaveCodeAsync("WAVE001", CancellationToken.None);

        Assert.Equal(0, result.IdentifiedCount);
        Assert.Equal(0, result.CleanedCount);
        Assert.Contains("关闭", result.Message);
    }

    /// <summary>
    /// 波次编码为空时，应返回跳过结论。
    /// </summary>
    [Fact]
    public async Task WaveCleanup_ShouldSkip_WhenWaveCodeEmpty()
    {
        var (service, _) = CreateWaveCleanupService();

        var result = await service.CleanByWaveCodeAsync("   ", CancellationToken.None);

        Assert.Equal(0, result.IdentifiedCount);
        Assert.Equal(0, result.CleanedCount);
    }

    /// <summary>
    /// 无非终态任务时，波次清理应返回零结果。
    /// </summary>
    [Fact]
    public async Task WaveCleanup_ShouldReturnZero_WhenNoNonTerminalTasks()
    {
        var (service, repo) = CreateWaveCleanupService();
        await repo.SaveAsync(BuildTask("T1", BusinessTaskStatus.Dropped, waveCode: "WAVE001"), CancellationToken.None);

        var result = await service.CleanByWaveCodeAsync("WAVE001", CancellationToken.None);

        Assert.Equal(0, result.IdentifiedCount);
        Assert.Equal(0, result.CleanedCount);
    }

    /// <summary>
    /// 有非终态任务时，波次清理应成功标记为异常状态。
    /// </summary>
    [Fact]
    public async Task WaveCleanup_ShouldClean_NonTerminalTasks()
    {
        var (service, repo) = CreateWaveCleanupService();
        await repo.SaveAsync(BuildTask("T1", BusinessTaskStatus.Created, waveCode: "WAVE001"), CancellationToken.None);
        await repo.SaveAsync(BuildTask("T2", BusinessTaskStatus.Scanned, waveCode: "WAVE001"), CancellationToken.None);
        await repo.SaveAsync(BuildTask("T3", BusinessTaskStatus.Dropped, waveCode: "WAVE001"), CancellationToken.None);

        var result = await service.CleanByWaveCodeAsync("WAVE001", CancellationToken.None);

        Assert.Equal(2, result.IdentifiedCount);
        Assert.Equal(2, result.CleanedCount);
        Assert.False(result.IsDryRun);

        var t1 = await repo.FindByTaskCodeAsync("T1", CancellationToken.None);
        var t2 = await repo.FindByTaskCodeAsync("T2", CancellationToken.None);
        Assert.Equal(BusinessTaskStatus.Exception, t1!.Status);
        Assert.Equal(BusinessTaskStatus.Exception, t2!.Status);
    }

    /// <summary>
    /// TargetStatusOnCleanup 配置为合法枚举值时，清理应按配置目标状态执行（此处以 Exception 为例）。
    /// </summary>
    [Fact]
    public async Task WaveCleanup_ShouldUseConfiguredTargetStatus()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var options = new ExceptionRuleOptions
        {
            Enabled = true,
            DryRun = false,
            WaveCleanup = new WaveCleanupRuleOptions { Enabled = true, TargetStatusOnCleanup = "Exception" }
        };
        var service = new WaveCleanupService(repo, options, NullLogger<WaveCleanupService>.Instance);

        await repo.SaveAsync(BuildTask("T1", BusinessTaskStatus.Created, waveCode: "WAVE002"), CancellationToken.None);

        var result = await service.CleanByWaveCodeAsync("WAVE002", CancellationToken.None);

        Assert.Equal(1, result.CleanedCount);
        var t1 = await repo.FindByTaskCodeAsync("T1", CancellationToken.None);
        Assert.Equal(BusinessTaskStatus.Exception, t1!.Status);
    }

    /// <summary>
    /// TargetStatusOnCleanup 配置为非法值时，清理应回退为 Exception 并正常执行（不抛出异常）。
    /// </summary>
    [Fact]
    public async Task WaveCleanup_ShouldFallbackToException_WhenTargetStatusInvalid()
    {
        var repo = new InMemoryBusinessTaskRepository();
        var options = new ExceptionRuleOptions
        {
            Enabled = true,
            DryRun = false,
            WaveCleanup = new WaveCleanupRuleOptions { Enabled = true, TargetStatusOnCleanup = "InvalidStatus" }
        };
        var service = new WaveCleanupService(repo, options, NullLogger<WaveCleanupService>.Instance);

        await repo.SaveAsync(BuildTask("T1", BusinessTaskStatus.Created, waveCode: "WAVE003"), CancellationToken.None);

        var result = await service.CleanByWaveCodeAsync("WAVE003", CancellationToken.None);

        Assert.Equal(1, result.CleanedCount);
        var t1 = await repo.FindByTaskCodeAsync("T1", CancellationToken.None);
        Assert.Equal(BusinessTaskStatus.Exception, t1!.Status);
    }

    /// <summary>
    /// dry-run 模式下，波次清理应识别任务但不执行状态变更。
    /// </summary>
    [Fact]
    public async Task WaveCleanup_ShouldNotClean_WhenDryRun()
    {
        var (service, repo) = CreateWaveCleanupService(dryRun: true);
        await repo.SaveAsync(BuildTask("T1", BusinessTaskStatus.Created, waveCode: "WAVE001"), CancellationToken.None);

        var result = await service.CleanByWaveCodeAsync("WAVE001", CancellationToken.None);

        Assert.Equal(1, result.IdentifiedCount);
        Assert.Equal(0, result.CleanedCount);
        Assert.True(result.IsDryRun);

        var t1 = await repo.FindByTaskCodeAsync("T1", CancellationToken.None);
        Assert.Equal(BusinessTaskStatus.Created, t1!.Status);
    }

    #endregion

    #region 多标签决策测试

    /// <summary>
    /// 规则总开关关闭时，多标签决策应返回非多标签结论。
    /// </summary>
    [Fact]
    public async Task MultiLabel_ShouldSkip_WhenDisabled()
    {
        var (service, _) = CreateMultiLabelService(enabled: false);

        var result = await service.DecideAsync("BC001", CancellationToken.None);

        Assert.False(result.IsMultiLabel);
        Assert.True(result.IsDecisionMade);
    }

    /// <summary>
    /// 只有一个活跃任务时，应返回非多标签结论且直接返回该任务编码。
    /// </summary>
    [Fact]
    public async Task MultiLabel_ShouldReturnSingleTask_WhenOnlyOneActive()
    {
        var (service, repo) = CreateMultiLabelService();
        await repo.SaveAsync(BuildTask("T1", BusinessTaskStatus.Created, barcode: "BC001"), CancellationToken.None);

        var result = await service.DecideAsync("BC001", CancellationToken.None);

        Assert.False(result.IsMultiLabel);
        Assert.True(result.IsDecisionMade);
        Assert.Equal("T1", result.SelectedTaskCode);
    }

    /// <summary>
    /// 策略 MarkException 下，多标签场景应标记为无法决策。
    /// </summary>
    [Fact]
    public async Task MultiLabel_ShouldMarkException_WhenStrategyIsMarkException()
    {
        var (service, repo) = CreateMultiLabelService(strategy: "MarkException");
        await repo.SaveAsync(BuildTask("T1", BusinessTaskStatus.Created, barcode: "BC001"), CancellationToken.None);
        await repo.SaveAsync(BuildTask("T2", BusinessTaskStatus.Scanned, barcode: "BC001"), CancellationToken.None);

        var result = await service.DecideAsync("BC001", CancellationToken.None);

        Assert.True(result.IsMultiLabel);
        Assert.False(result.IsDecisionMade);
        Assert.Equal(2, result.DiscardedTaskCodes.Count);
    }

    /// <summary>
    /// 策略 UseFirst 下，应选用创建时间最早的任务，舍弃其余任务。
    /// </summary>
    [Fact]
    public async Task MultiLabel_ShouldUseFirst_WhenStrategyIsUseFirst()
    {
        var (service, repo) = CreateMultiLabelService(strategy: "UseFirst");

        var t1 = BuildTask("T1", BusinessTaskStatus.Created, barcode: "BC001");
        t1.CreatedTimeLocal = DateTime.Now.AddMinutes(-5);
        await repo.SaveAsync(t1, CancellationToken.None);

        var t2 = BuildTask("T2", BusinessTaskStatus.Scanned, barcode: "BC001");
        t2.CreatedTimeLocal = DateTime.Now;
        await repo.SaveAsync(t2, CancellationToken.None);

        var result = await service.DecideAsync("BC001", CancellationToken.None);

        Assert.True(result.IsMultiLabel);
        Assert.True(result.IsDecisionMade);
        Assert.Equal("T1", result.SelectedTaskCode);
        Assert.Single(result.DiscardedTaskCodes);
        Assert.Equal("T2", result.DiscardedTaskCodes[0]);
    }

    /// <summary>
    /// 策略 UseLatest 下，应选用创建时间最晚的任务，舍弃其余任务。
    /// </summary>
    [Fact]
    public async Task MultiLabel_ShouldUseLatest_WhenStrategyIsUseLatest()
    {
        var (service, repo) = CreateMultiLabelService(strategy: "UseLatest");

        var t1 = BuildTask("T1", BusinessTaskStatus.Created, barcode: "BC001");
        t1.CreatedTimeLocal = DateTime.Now.AddMinutes(-5);
        await repo.SaveAsync(t1, CancellationToken.None);

        var t2 = BuildTask("T2", BusinessTaskStatus.Scanned, barcode: "BC001");
        t2.CreatedTimeLocal = DateTime.Now;
        await repo.SaveAsync(t2, CancellationToken.None);

        var result = await service.DecideAsync("BC001", CancellationToken.None);

        Assert.True(result.IsMultiLabel);
        Assert.True(result.IsDecisionMade);
        Assert.Equal("T2", result.SelectedTaskCode);
        Assert.Single(result.DiscardedTaskCodes);
        Assert.Equal("T1", result.DiscardedTaskCodes[0]);
    }

    /// <summary>
    /// 终态任务不应被纳入多标签计数。
    /// </summary>
    [Fact]
    public async Task MultiLabel_ShouldIgnoreTerminalTasks()
    {
        var (service, repo) = CreateMultiLabelService(strategy: "MarkException");
        await repo.SaveAsync(BuildTask("T1", BusinessTaskStatus.Created, barcode: "BC001"), CancellationToken.None);
        await repo.SaveAsync(BuildTask("T2", BusinessTaskStatus.Dropped, barcode: "BC001"), CancellationToken.None);
        await repo.SaveAsync(BuildTask("T3", BusinessTaskStatus.Exception, barcode: "BC001"), CancellationToken.None);

        var result = await service.DecideAsync("BC001", CancellationToken.None);

        Assert.False(result.IsMultiLabel);
        Assert.True(result.IsDecisionMade);
        Assert.Equal("T1", result.SelectedTaskCode);
    }

    #endregion

    #region 回流规则测试

    /// <summary>
    /// 规则总开关关闭时，回流规则应返回不回流结论。
    /// </summary>
    [Fact]
    public async Task Recirculation_ShouldSkip_WhenDisabled()
    {
        var (service, repo) = CreateRecirculationService(enabled: false);
        var task = BuildTask("T1", BusinessTaskStatus.Scanned, scanRetryCount: 10);
        await repo.SaveAsync(task, CancellationToken.None);

        var result = await service.EvaluateAsync(task.Id, CancellationToken.None);

        Assert.False(result.ShouldRecirculate);
    }

    /// <summary>
    /// 任务不存在时，回流规则应返回不回流结论。
    /// </summary>
    [Fact]
    public async Task Recirculation_ShouldReturnFalse_WhenTaskNotFound()
    {
        var (service, _) = CreateRecirculationService();

        var result = await service.EvaluateAsync(9999L, CancellationToken.None);

        Assert.False(result.ShouldRecirculate);
    }

    /// <summary>
    /// 扫描重试次数未超出上限时，不触发回流。
    /// </summary>
    [Fact]
    public async Task Recirculation_ShouldNotRecirculate_WhenRetryCountBelowMax()
    {
        var (service, repo) = CreateRecirculationService(maxRetries: 3);
        var task = BuildTask("T1", BusinessTaskStatus.Scanned, scanRetryCount: 2);
        await repo.SaveAsync(task, CancellationToken.None);

        var result = await service.EvaluateAsync(task.Id, CancellationToken.None);

        Assert.False(result.ShouldRecirculate);
        Assert.Equal(2, result.ScanRetryCount);
    }

    /// <summary>
    /// 扫描重试次数达到上限时，应触发回流并将任务标记为回流状态。
    /// </summary>
    [Fact]
    public async Task Recirculation_ShouldRecirculate_WhenRetryCountReachesMax()
    {
        var (service, repo) = CreateRecirculationService(maxRetries: 3);
        var task = BuildTask("T1", BusinessTaskStatus.Scanned, scanRetryCount: 3);
        await repo.SaveAsync(task, CancellationToken.None);

        var result = await service.EvaluateAsync(task.Id, CancellationToken.None);

        Assert.True(result.ShouldRecirculate);
        Assert.Equal(BusinessTaskStatus.Exception, result.RecommendedStatus);

        var updated = await repo.FindByTaskCodeAsync("T1", CancellationToken.None);
        Assert.True(updated!.IsRecirculated);
        Assert.Equal(BusinessTaskStatus.Exception, updated.Status);
    }

    /// <summary>
    /// dry-run 模式下，回流规则应识别触发条件但不执行状态变更。
    /// </summary>
    [Fact]
    public async Task Recirculation_ShouldNotUpdate_WhenDryRun()
    {
        var (service, repo) = CreateRecirculationService(maxRetries: 3, dryRun: true);
        var task = BuildTask("T1", BusinessTaskStatus.Scanned, scanRetryCount: 5);
        await repo.SaveAsync(task, CancellationToken.None);

        var result = await service.EvaluateAsync(task.Id, CancellationToken.None);

        Assert.True(result.ShouldRecirculate);
        Assert.Contains("DryRun", result.Reason);

        var notUpdated = await repo.FindByTaskCodeAsync("T1", CancellationToken.None);
        Assert.False(notUpdated!.IsRecirculated);
        Assert.Equal(BusinessTaskStatus.Scanned, notUpdated.Status);
    }

    #endregion
}
