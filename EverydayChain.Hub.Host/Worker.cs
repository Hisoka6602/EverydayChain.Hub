using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Host;

/// <summary>
/// 后台工作服务，定期向分拣任务追踪表写入演示数据，验证分表自治与自动调谐机制。
/// </summary>
public class Worker(ILogger<Worker> logger, ISortingTaskTraceWriter sortingTaskTraceWriter, ISqlExecutionTuner tuner, IOptions<WorkerOptions> workerOptions) : BackgroundService
{
    /// <summary>后台工作服务配置快照。</summary>
    private readonly WorkerOptions _workerOptions = workerOptions.Value;

    /// <summary>
    /// 后台循环主体：按配置的轮询间隔构造一条演示记录并调用写入服务。
    /// </summary>
    /// <param name="stoppingToken">宿主停止时触发的取消令牌。</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var demoData = new List<SortingTaskTraceEntity>
            {
                new()
                {
                    BusinessNo = $"BIZ-{DateTimeOffset.Now:yyyyMMddHHmmss}",
                    Channel = "WMS",
                    StationCode = "ST-01",
                    Status = "Created",
                    CreatedAt = DateTimeOffset.Now,
                    Payload = "自动调谐与分表自治演示写入"
                }
            };

            try
            {
                await sortingTaskTraceWriter.WriteAsync(demoData, stoppingToken);
                logger.LogInformation("后台任务写入成功，当前调谐批量窗口: {BatchSize}", tuner.CurrentBatchSize);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "后台任务写入失败，将在下次循环重试。");
            }

            await Task.Delay(TimeSpan.FromSeconds(_workerOptions.PollingIntervalSeconds), stoppingToken);
        }
    }
}
