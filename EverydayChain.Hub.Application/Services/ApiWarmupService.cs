using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// API 启动预热服务实现。
/// </summary>
public sealed class ApiWarmupService(
    IGlobalDashboardQueryService globalDashboardQueryService,
    IDockDashboardQueryService dockDashboardQueryService,
    IWaveQueryService waveQueryService,
    IBusinessTaskRepository businessTaskRepository,
    ILogger<ApiWarmupService> logger) : IApiWarmupService
{
    /// <summary>波次查询预热占位波次编码。</summary>
    private const string WarmupWaveCode = "WARMUP";
    /// <summary>条码查询预热占位文本。</summary>
    private const string WarmupBarcode = "WARMUP-BARCODE";
    /// <summary>任务号查询预热占位文本。</summary>
    private const string WarmupTaskCode = "WARMUP-TASK";
    /// <summary>来源表查询预热占位文本。</summary>
    private const string WarmupSourceTableCode = "WARMUP_SOURCE";
    /// <summary>业务键查询预热占位文本。</summary>
    private const string WarmupBusinessKey = "WARMUP_KEY";

    /// <inheritdoc/>
    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var startTimeLocal = now.AddHours(-1);
        var endTimeLocal = now.AddHours(1);

        await TryWarmupStepAsync(
            "总看板查询链路",
            async () => await globalDashboardQueryService.QueryAsync(
                new GlobalDashboardQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                cancellationToken),
            cancellationToken);
        await TryWarmupStepAsync(
            "码头看板查询链路",
            async () => await dockDashboardQueryService.QueryAsync(
                new DockDashboardQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                cancellationToken),
            cancellationToken);
        await TryWarmupStepAsync(
            "波次选项查询链路",
            async () => await waveQueryService.QueryOptionsAsync(
                new WaveOptionsQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal
                },
                cancellationToken),
            cancellationToken);
        await TryWarmupStepAsync(
            "波次摘要查询链路",
            async () => await waveQueryService.QuerySummaryAsync(
                new WaveSummaryQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal,
                    WaveCode = WarmupWaveCode
                },
                cancellationToken),
            cancellationToken);
        await TryWarmupStepAsync(
            "波次分区查询链路",
            async () => await waveQueryService.QueryZonesAsync(
                new WaveZoneQueryRequest
                {
                    StartTimeLocal = startTimeLocal,
                    EndTimeLocal = endTimeLocal,
                    WaveCode = WarmupWaveCode
                },
                cancellationToken),
            cancellationToken);
        await TryWarmupStepAsync(
            "高频仓储定位查询链路",
            async () =>
            {
                await businessTaskRepository.FindByBarcodeAsync(WarmupBarcode, cancellationToken);
                await businessTaskRepository.FindByTaskCodeAsync(WarmupTaskCode, cancellationToken);
                await businessTaskRepository.FindBySourceTableAndBusinessKeyAsync(WarmupSourceTableCode, WarmupBusinessKey, cancellationToken);
            },
            cancellationToken);
        logger.LogInformation("启动预热执行完成。");
    }

    /// <summary>
    /// 执行单个预热步骤，失败时记录日志并继续后续步骤。
    /// </summary>
    /// <param name="stepName">步骤名称。</param>
    /// <param name="action">步骤动作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task TryWarmupStepAsync(string stepName, Func<Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await action();
            logger.LogInformation("启动预热步骤完成：{StepName}", stepName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "启动预热步骤失败，已跳过：{StepName}", stepName);
        }
    }
}
