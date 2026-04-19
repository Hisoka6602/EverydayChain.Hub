using EverydayChain.Hub.Application.Abstractions.Integrations;
using EverydayChain.Hub.Application.Feedback.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Integrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 业务回传服务单元测试。
/// </summary>
public sealed class WmsFeedbackServiceTests
{
    /// <summary>
    /// 构建测试用的 WmsFeedbackService，注入内存替身，默认 Enabled=true。
    /// </summary>
    private static (WmsFeedbackService Service, InMemoryBusinessTaskRepository Repository, CapturingWmsOracleFeedbackGateway Writer) CreateService(bool enabled = true)
    {
        var repo = new InMemoryBusinessTaskRepository();
        var writer = new CapturingWmsOracleFeedbackGateway();
        var options = new WmsFeedbackOptions { Enabled = enabled };
        var service = new WmsFeedbackService(repo, writer, options, NullLogger<WmsFeedbackService>.Instance);
        return (service, repo, writer);
    }

    /// <summary>
    /// 无待回传任务时应返回空结果且不调用写入器。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenNoPendingTasks()
    {
        var (service, _, writer) = CreateService();

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(0, result.PendingCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.IsSuccess);
        Assert.Empty(writer.CapturedTasks);
    }

    /// <summary>
    /// 有待回传任务且写入成功时，任务应标记为已回传。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldMarkCompleted_WhenWriterSucceeds()
    {
        var (service, repo, writer) = CreateService();

        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-001",
            SourceTableCode = "WMS",
            BusinessKey = "K1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        writer.ReturnCount = 1;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.PendingCount);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.IsSuccess);

        var updated = await repo.FindByTaskCodeAsync("TASK-001", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Completed, updated!.FeedbackStatus);
        Assert.True(updated.IsFeedbackReported);
        Assert.NotNull(updated.FeedbackTimeLocal);
    }

    /// <summary>
    /// 写入器抛出异常时，所有任务应标记为回传失败。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldMarkFailed_WhenWriterThrows()
    {
        var (service, repo, writer) = CreateService();

        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-002",
            SourceTableCode = "WMS",
            BusinessKey = "K2",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        writer.ThrowOnWrite = true;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.PendingCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.False(result.IsSuccess);

        var updated = await repo.FindByTaskCodeAsync("TASK-002", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Failed, updated!.FeedbackStatus);
        Assert.False(updated.IsFeedbackReported);
        Assert.Null(updated.FeedbackTimeLocal);
    }

    /// <summary>
    /// batchSize 参数应限制查询上限。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldRespectBatchSize()
    {
        var (service, repo, writer) = CreateService();
        writer.ReturnCount = 2;

        for (var i = 1; i <= 5; i++)
        {
            await repo.SaveAsync(new BusinessTaskEntity
            {
                TaskCode = $"TASK-{i:D3}",
                SourceTableCode = "WMS",
                BusinessKey = $"K{i}",
                Status = BusinessTaskStatus.Dropped,
                FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
                CreatedTimeLocal = DateTime.Now,
                UpdatedTimeLocal = DateTime.Now
            }, CancellationToken.None);
        }

        var result = await service.ExecuteAsync(2, CancellationToken.None);

        Assert.Equal(2, result.PendingCount);
        Assert.Equal(2, writer.CapturedTasks.Count);
    }

    /// <summary>
    /// 回传开关关闭时应直接返回空结果，不消费任何待回传任务。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenDisabled()
    {
        var (service, repo, writer) = CreateService(enabled: false);

        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-D01",
            SourceTableCode = "WMS",
            BusinessKey = "KD1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        // 开关关闭时应直接短路，不应查询或更新任何任务。
        Assert.Equal(0, result.PendingCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.IsSuccess);
        Assert.Empty(writer.CapturedTasks);

        // 任务状态应保持 Pending 不变（未被消费）。
        var task = await repo.FindByTaskCodeAsync("TASK-D01", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Pending, task!.FeedbackStatus);
    }

    /// <summary>
    /// Oracle 返回行数与任务数不一致时，应按整批失败处理。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldMarkFailed_WhenWrittenRowsMismatch()
    {
        var (service, repo, writer) = CreateService();

        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-M01",
            SourceTableCode = "WMS",
            BusinessKey = "KM1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);

        // 模拟 Oracle 返回 0 行（行数与任务数不一致）。
        writer.ReturnCount = 0;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.PendingCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.False(result.IsSuccess);

        var updated = await repo.FindByTaskCodeAsync("TASK-M01", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Failed, updated!.FeedbackStatus);
        Assert.False(updated.IsFeedbackReported);
        Assert.Null(updated.FeedbackTimeLocal);
    }

    /// <summary>
    /// 拆零任务回写成功时，应更新为已回传状态。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldCompleteSplitTask_WhenSplitTaskWritten()
    {
        var (service, repo, writer) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-SPLIT-001",
            SourceTableCode = "WMS_SPLIT",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "KS1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);
        writer.ReturnCount = 1;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var updated = await repo.FindByTaskCodeAsync("TASK-SPLIT-001", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Completed, updated!.FeedbackStatus);
        Assert.True(updated.IsFeedbackReported);
        Assert.Contains(writer.CapturedTasks, task => task.SourceType == BusinessTaskSourceType.Split);
    }

    /// <summary>
    /// 整件任务回写成功时，应更新为已回传状态。
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ShouldCompleteFullCaseTask_WhenFullCaseTaskWritten()
    {
        var (service, repo, writer) = CreateService();
        await repo.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "TASK-FULL-001",
            SourceTableCode = "WMS_FULLCASE",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "KF1",
            Status = BusinessTaskStatus.Dropped,
            FeedbackStatus = BusinessTaskFeedbackStatus.Pending,
            CreatedTimeLocal = DateTime.Now,
            UpdatedTimeLocal = DateTime.Now
        }, CancellationToken.None);
        writer.ReturnCount = 1;

        var result = await service.ExecuteAsync(100, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var updated = await repo.FindByTaskCodeAsync("TASK-FULL-001", CancellationToken.None);
        Assert.Equal(BusinessTaskFeedbackStatus.Completed, updated!.FeedbackStatus);
        Assert.True(updated.IsFeedbackReported);
        Assert.Contains(writer.CapturedTasks, task => task.SourceType == BusinessTaskSourceType.FullCase);
    }

    /// <summary>
    /// 验证回写网关按来源类型分流到拆零与整件目标表的行为。
    /// </summary>
    [Fact]
    public void ResolveTargetBySourceType_ShouldRouteToConfiguredTables()
    {
        var gateway = CreateGatewayForSourceTypeRoutingTests();
        var resolveMethod = GetResolveTargetBySourceTypeMethod();

        var splitTarget = ((string Schema, string Table, string BusinessKeyColumn))resolveMethod.Invoke(gateway, [BusinessTaskSourceType.Split])!;
        var fullCaseTarget = ((string Schema, string Table, string BusinessKeyColumn))resolveMethod.Invoke(gateway, [BusinessTaskSourceType.FullCase])!;

        Assert.Equal("WMS_USER_SPLIT_431", splitTarget.Schema);
        Assert.Equal("IDX_PICKTOLIGHT_CARTON1", splitTarget.Table);
        Assert.Equal("SPLIT_TASK_CODE", splitTarget.BusinessKeyColumn);
        Assert.Equal("WMS_USER_FULLCASE_431", fullCaseTarget.Schema);
        Assert.Equal("IDX_PICKTOWCS2", fullCaseTarget.Table);
        Assert.Equal("FULLCASE_TASK_CODE", fullCaseTarget.BusinessKeyColumn);
    }

    /// <summary>
    /// 验证回写网关遇到非法来源类型时抛出中文异常且不回退默认目标表的行为。
    /// </summary>
    [Fact]
    public void ResolveTargetBySourceType_ShouldThrowChineseException_WhenSourceTypeUnsupported()
    {
        var gateway = CreateGatewayForSourceTypeRoutingTests();
        var resolveMethod = GetResolveTargetBySourceTypeMethod();

        var exception = Assert.Throws<TargetInvocationException>(() =>
            resolveMethod.Invoke(gateway, [BusinessTaskSourceType.Unknown]));
        var innerException = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("不支持的业务来源类型，无法确定 WMS 回写目标表。", innerException.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 创建用于来源分流测试的回写网关实例。
    /// </summary>
    /// <returns>回写网关。</returns>
    private static OracleWmsFeedbackGateway CreateGatewayForSourceTypeRoutingTests()
    {
        var options = Options.Create(new WmsFeedbackOptions
        {
            Enabled = true,
            SplitSchema = "WMS_USER_SPLIT_431",
            SplitTable = "IDX_PICKTOLIGHT_CARTON1",
            SplitBusinessKeyColumn = "SPLIT_TASK_CODE",
            FullCaseSchema = "WMS_USER_FULLCASE_431",
            FullCaseTable = "IDX_PICKTOWCS2",
            FullCaseBusinessKeyColumn = "FULLCASE_TASK_CODE",
            FeedbackStatusColumn = "STATUS",
            FeedbackCompletedValue = "Y"
        });
        var oracleOptions = Options.Create(new OracleOptions
        {
            ConnectionString = "Data Source=127.0.0.1:1521/ORCL;User Id=PLACEHOLDER_USER;Password=PLACEHOLDER_PASSWORD;"
        });
        return new OracleWmsFeedbackGateway(
            options,
            oracleOptions,
            new PassThroughDangerZoneExecutor(),
            NullLogger<OracleWmsFeedbackGateway>.Instance);
    }

    /// <summary>
    /// 通过反射获取来源分流私有方法句柄。
    /// </summary>
    /// <returns>方法句柄。</returns>
    private static MethodInfo GetResolveTargetBySourceTypeMethod()
    {
        return typeof(OracleWmsFeedbackGateway).GetMethod(
                "ResolveTargetBySourceType",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到 ResolveTargetBySourceType 方法。");
    }
}

/// <summary>
/// 捕获写入请求的 WMS Oracle 网关测试替身。
/// </summary>
internal sealed class CapturingWmsOracleFeedbackGateway : IWmsOracleFeedbackGateway
{
    /// <summary>捕获到的任务列表。</summary>
    public List<BusinessTaskEntity> CapturedTasks { get; } = [];

    /// <summary>模拟网关抛出异常。</summary>
    public bool ThrowOnWrite { get; set; }

    /// <summary>网关返回的行数；默认与输入列表长度相同。</summary>
    public int? ReturnCount { get; set; }

    /// <inheritdoc/>
    public Task<int> WriteFeedbackAsync(IReadOnlyList<BusinessTaskEntity> tasks, CancellationToken ct)
    {
        CapturedTasks.AddRange(tasks);

        if (ThrowOnWrite)
        {
            throw new InvalidOperationException("模拟 WMS Oracle 网关异常。");
        }

        return Task.FromResult(ReturnCount ?? tasks.Count);
    }
}
