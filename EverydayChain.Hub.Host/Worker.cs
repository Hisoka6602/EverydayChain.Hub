using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Host
{
    /// <summary>
    /// 后台工作服务。
    /// </summary>
    public class Worker : BackgroundService
    {
        /// <summary>
        /// 日志记录器。
        /// </summary>
        private readonly ILogger<Worker> _logger;

        /// <summary>
        /// 分拣任务轨迹写入器。
        /// </summary>
        private readonly ISortingTaskTraceWriter _sortingTaskTraceWriter;

        /// <summary>
        /// SQL 执行调谐器。
        /// </summary>
        private readonly ISqlExecutionTuner _tuner;

        /// <summary>
        /// 初始化后台工作服务。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="sortingTaskTraceWriter">分拣任务轨迹写入器。</param>
        /// <param name="tuner">SQL 执行调谐器。</param>
        public Worker(
            ILogger<Worker> logger,
            ISortingTaskTraceWriter sortingTaskTraceWriter,
            ISqlExecutionTuner tuner)
        {
            _logger = logger;
            _sortingTaskTraceWriter = sortingTaskTraceWriter;
            _tuner = tuner;
        }

        /// <summary>
        /// 执行后台轮询任务。
        /// </summary>
        /// <param name="stoppingToken">停止令牌。</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 步骤1：循环监听宿主停止信号。
            while (!stoppingToken.IsCancellationRequested)
            {
                // 步骤2：构造演示写入数据，并统一采用本地时间语义。
                var demoData = new List<SortingTaskTraceEntity>
                {
                    new()
                    {
                        BusinessNo = $"BIZ-{DateTime.Now:yyyyMMddHHmmss}",
                        Channel = "WMS",
                        StationCode = "ST-01",
                        Status = "Created",
                        CreatedAt = DateTimeOffset.Now,
                        Payload = "自动调谐与分表自治演示写入"
                    }
                };

                // 步骤3：执行写入并记录调谐窗口信息。
                try
                {
                    await _sortingTaskTraceWriter.WriteAsync(demoData, stoppingToken);
                    _logger.LogInformation("后台任务写入成功，当前调谐批量窗口: {BatchSize}", _tuner.CurrentBatchSize);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "后台任务写入失败，将在下次循环重试。");
                }

                // 步骤4：按固定间隔等待下一次执行。
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
