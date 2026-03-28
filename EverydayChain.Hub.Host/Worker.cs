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
        /// 初始化后台工作服务。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
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
                // 步骤2：在信息日志级别启用时输出当前本地时间。
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                // 步骤3：按固定间隔等待下一次执行。
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
