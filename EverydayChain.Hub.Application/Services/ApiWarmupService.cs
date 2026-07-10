using System.Diagnostics;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 启动查询链路预热服务。
/// </summary>
public sealed class ApiWarmupService(
    IGlobalDashboardQueryService globalDashboardQueryService,
    IDockDashboardQueryService dockDashboardQueryService,
    IWaveQueryService waveQueryService,
    IBusinessTaskReadService businessTaskReadService,
    IRecirculationQueryService recirculationQueryService,
    ISortingReportQueryService sortingReportQueryService,
    IBoxTrackingQueryService boxTrackingQueryService,
    IChuteQueryService chuteQueryService,
    IExportCatalogQueryService exportCatalogQueryService,
    IRetentionCleanupQueryService retentionCleanupQueryService,
    ILogger<ApiWarmupService> logger) : IApiWarmupService
{
    /// <summary>
    /// 存储 WarmupWaveCode 字段。
    /// </summary>
    private const string WarmupWaveCode = "WARMUP";
    /// <summary>
    /// 存储 WarmupBarcode 字段。
    /// </summary>
    private const string WarmupBarcode = "WARMUP-BARCODE";
    /// <summary>
    /// 存储 WarmupTaskCode 字段。
    /// </summary>
    private const string WarmupTaskCode = "WARMUP-TASK";
    /// <summary>
    /// 存储 WarmupOrderId 字段。
    /// </summary>
    private const string WarmupOrderId = "WARMUP-ORDER";
    /// <summary>
    /// 存储 WarmupPageSize 字段。
    /// </summary>
    private const int WarmupPageSize = 100;
    /// <summary>
    /// 存储 WarmupLookbackHours 字段。
    /// </summary>
    private const int WarmupLookbackHours = 24;
    /// <summary>
    /// 存储 WarmupMaxParallelSteps 字段。
    /// </summary>
    private const int WarmupMaxParallelSteps = 4;
    /// <summary>
    /// 存储 WarmupCursorLastId 字段。
    /// </summary>
    private const long WarmupCursorLastId = long.MaxValue;

    /// <summary>
    /// 执行 WarmupAsync 方法。
    /// </summary>
    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        // 步骤：先发现可复用的真实业务上下文，再并发预热重查询链路，确保后续首个真实请求更容易直接命中缓存。
        var now = TruncateToSecond(DateTime.Now);
        var startTimeLocal = now.AddHours(-WarmupLookbackHours);
        var endTimeLocal = now;
        var stopwatch = Stopwatch.StartNew();

        var discovery = await DiscoverWarmupContextAsync(startTimeLocal, endTimeLocal, cancellationToken);
        var steps = BuildWarmupSteps(startTimeLocal, endTimeLocal, discovery);

        logger.LogInformation(
            "启动预热执行开始。StepCount={StepCount}, MaxParallelSteps={MaxParallelSteps}, StartTimeLocal={StartTimeLocal}, EndTimeLocal={EndTimeLocal}, WaveCode={WaveCode}, TaskCode={TaskCode}, Barcode={Barcode}, OrderId={OrderId}",
            steps.Length,
            WarmupMaxParallelSteps,
            startTimeLocal,
            endTimeLocal,
            discovery.WaveCode,
            discovery.TaskCode,
            discovery.Barcode,
            discovery.OrderId ?? string.Empty);

        await Parallel.ForEachAsync(
            steps,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = WarmupMaxParallelSteps,
                CancellationToken = cancellationToken
            },
            async (step, ct) => await TryWarmupStepAsync(step.Name, step.Action, ct));

        stopwatch.Stop();
        logger.LogInformation(
            "启动预热执行完成。StepCount={StepCount}, ElapsedMilliseconds={ElapsedMilliseconds}",
            steps.Length,
            stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// 执行 DiscoverWarmupContextAsync 方法。
    /// </summary>
    private async Task<WarmupDiscoveryContext> DiscoverWarmupContextAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        CancellationToken cancellationToken)
    {
        // 步骤：先用轻量查询发现当前真实的波次、任务和条码上下文，避免预热只命中占位参数而无法覆盖真实查询。
        var currentWaveTask = TryRunDiscoveryAsync(
            "波次当前查询链路",
            ct => waveQueryService.QueryCurrentAsync(
                new CurrentWaveQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                ct),
            cancellationToken);
        var waveOptionsTask = TryRunDiscoveryAsync(
            "波次选项查询链路",
            ct => waveQueryService.QueryOptionsAsync(
                new WaveOptionsQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                ct),
            cancellationToken);
        var waveListTask = TryRunDiscoveryAsync(
            "波次列表查询链路",
            ct => waveQueryService.QueryListAsync(
                new WaveListQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                ct),
            cancellationToken);
        var businessTasksTask = TryRunDiscoveryAsync(
            "业务任务分页查询链路",
            ct => businessTaskReadService.QueryTasksAsync(
                new BusinessTaskQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal,
                    PageNumber = 1,
                    PageSize = WarmupPageSize
                },
                ct),
            cancellationToken);

        await Task.WhenAll(currentWaveTask, waveOptionsTask, waveListTask, businessTasksTask);

        var currentWave = await currentWaveTask;
        var waveOptions = await waveOptionsTask;
        var waveList = await waveListTask;
        var businessTasks = await businessTasksTask;
        var firstTaskItem = businessTasks?.Items.FirstOrDefault();

        var discoveredWaveCode =
            currentWave?.WaveCode
            ?? waveOptions?.WaveOptions.FirstOrDefault()?.WaveCode
            ?? waveList?.Items.FirstOrDefault()?.WaveCode
            ?? firstTaskItem?.WaveCode
            ?? WarmupWaveCode;
        var discoveredTaskCode =
            firstTaskItem?.TaskCode
            ?? WarmupTaskCode;
        var discoveredBarcode =
            firstTaskItem?.Barcode
            ?? currentWave?.Barcode
            ?? WarmupBarcode;
        var discoveredOrderId = firstTaskItem?.OrderId;

        return new WarmupDiscoveryContext(
            discoveredWaveCode,
            discoveredTaskCode,
            discoveredBarcode,
            discoveredOrderId);
    }

    /// <summary>
    /// 执行 BuildWarmupSteps 方法。
    /// </summary>
    private (string Name, Func<CancellationToken, Task> Action)[] BuildWarmupSteps(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        WarmupDiscoveryContext discovery)
    {
        // 步骤：统一维护需要持续保温的重查询链路，确保启动预热与周期保温使用完全一致的参数策略。
        return
        [
            (
                "总看板查询链路",
                async ct => await globalDashboardQueryService.QueryAsync(
                    new GlobalDashboardQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal
                    },
                    ct)
            ),
            (
                "码头看板查询链路（自动波次）",
                async ct => await dockDashboardQueryService.QueryAsync(
                    new DockDashboardQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal
                    },
                    ct)
            ),
            (
                "码头看板查询链路（指定波次）",
                async ct => await dockDashboardQueryService.QueryAsync(
                    new DockDashboardQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        WaveCode = discovery.WaveCode
                    },
                    ct)
            ),
            (
                "波次摘要查询链路",
                async ct => await waveQueryService.QuerySummaryAsync(
                    new WaveSummaryQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        WaveCode = discovery.WaveCode
                    },
                    ct)
            ),
            (
                "波次分区查询链路",
                async ct => await waveQueryService.QueryZonesAsync(
                    new WaveZoneQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        WaveCode = discovery.WaveCode
                    },
                    ct)
            ),
            (
                "波次明细查询链路",
                async ct => await waveQueryService.QueryDetailsAsync(
                    new WaveDetailQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        WaveCode = discovery.WaveCode
                    },
                    ct)
            ),
            (
                "波次清理查询链路",
                async ct => await waveQueryService.QueryCleanupWaveAsync(discovery.WaveCode, ct)
            ),
            (
                "业务任务游标查询链路",
                async ct => await businessTaskReadService.QueryTasksAsync(
                    new BusinessTaskQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        PageNumber = 1,
                        PageSize = WarmupPageSize,
                        LastCreatedTimeLocal = endTimeLocal,
                        LastId = WarmupCursorLastId
                    },
                    ct)
            ),
            (
                "异常件查询链路",
                async ct => await businessTaskReadService.QueryExceptionsAsync(
                    new BusinessTaskQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        PageNumber = 1,
                        PageSize = WarmupPageSize
                    },
                    ct)
            ),
            (
                "回流件明细查询链路",
                async ct => await businessTaskReadService.QueryRecirculationsAsync(
                    new BusinessTaskQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        PageNumber = 1,
                        PageSize = WarmupPageSize
                    },
                    ct)
            ),
            (
                "回流汇总查询链路",
                async ct => await recirculationQueryService.QuerySummaryAsync(
                    new RecirculationSummaryQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        SortOrder = "Most"
                    },
                    ct)
            ),
            (
                "分拣报表查询链路",
                async ct => await sortingReportQueryService.QueryAsync(
                    new SortingReportQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal
                    },
                    ct)
            ),
            (
                "箱号追踪分页查询链路",
                async ct => await boxTrackingQueryService.QueryAsync(
                    new BoxTrackingQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        BoxId = discovery.Barcode,
                        PageNumber = 1,
                        PageSize = WarmupPageSize
                    },
                    ct)
            ),
            (
                "箱号追踪任务筛选查询链路",
                async ct => await boxTrackingQueryService.QueryAsync(
                    new BoxTrackingQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        BoxId = discovery.Barcode,
                        OrderId = discovery.OrderId ?? WarmupOrderId,
                        PageNumber = 1,
                        PageSize = WarmupPageSize
                    },
                    ct)
            ),
            (
                "格口解析查询链路",
                async ct => await chuteQueryService.ExecuteAsync(
                    new ChuteResolveApplicationRequest
                    {
                        TaskCode = discovery.TaskCode,
                        Barcode = discovery.Barcode
                    },
                    ct)
            ),
            (
                "导出目录查询链路",
                async ct => await exportCatalogQueryService.QueryAsync(
                    new ExportCatalogQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal
                    },
                    ct)
            ),
            (
                "保留期审计查询链路",
                async ct => await retentionCleanupQueryService.QueryAsync(
                    new RetentionCleanupAuditQueryRequest
                    {
                        StartTimeLocal = startTimeLocal,
                        EndTimeLocal = endTimeLocal,
                        PageNumber = 1,
                        PageSize = WarmupPageSize
                    },
                    ct)
            )
        ];
    }

    /// <summary>
    /// 执行 TryWarmupStepAsync 方法。
    /// </summary>
    private async Task TryWarmupStepAsync(
        string stepName,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        // 步骤：执行单个预热步骤并记录耗时；若步骤本身失败，则仅记录日志并继续预热其他链路。
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await action(cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "启动预热步骤完成。StepName={StepName}, ElapsedMilliseconds={ElapsedMilliseconds}",
                stepName,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWarning(
                ex,
                "启动预热步骤失败，已跳过。StepName={StepName}, ElapsedMilliseconds={ElapsedMilliseconds}",
                stepName,
                stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 执行 TryRunDiscoveryAsync 方法。
    /// </summary>
    private async Task<T?> TryRunDiscoveryAsync<T>(
        string stepName,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken) where T : class
    {
        // 步骤：执行单个上下文发现步骤并兜底吞掉非取消异常，保证发现链路局部失败时整体预热仍可继续。
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action(cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "启动预热步骤完成。StepName={StepName}, ElapsedMilliseconds={ElapsedMilliseconds}",
                stepName,
                stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWarning(
                ex,
                "启动预热步骤失败，已跳过。StepName={StepName}, ElapsedMilliseconds={ElapsedMilliseconds}",
                stepName,
                stopwatch.ElapsedMilliseconds);
            return default;
        }
    }

    /// <summary>
    /// 执行 TruncateToSecond 方法。
    /// </summary>
    private static DateTime TruncateToSecond(DateTime value)
    {
        return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind);
    }

    /// <summary>
    /// 定义 WarmupDiscoveryContext 类型。
    /// </summary>
    private readonly record struct WarmupDiscoveryContext(
        string WaveCode,
        string TaskCode,
        string Barcode,
        string? OrderId);
}
