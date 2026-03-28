using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Host;

public class Worker(ILogger<Worker> logger, ISortingTaskTraceWriter sortingTaskTraceWriter, ISqlExecutionTuner tuner) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            var demoData = new List<SortingTaskTraceEntity> {
                new() {
                    BusinessNo = $"BIZ-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
                    Channel = "WMS",
                    StationCode = "ST-01",
                    Status = "Created",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Payload = "自动调谐与分表自治演示写入"
                }
            };

            await sortingTaskTraceWriter.WriteAsync(demoData, stoppingToken);
            logger.LogInformation("后台任务写入成功，当前调谐批量窗口: {BatchSize}", tuner.CurrentBatchSize);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
