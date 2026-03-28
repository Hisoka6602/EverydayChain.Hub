namespace EverydayChain.Hub.Host.Options;

/// <summary>
/// 后台工作服务配置，从 <c>appsettings.json</c> 的 <c>Worker</c> 节点绑定。
/// </summary>
public class WorkerOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "Worker";

    /// <summary>后台任务每轮循环的轮询间隔（秒），默认 10 秒。</summary>
    public int PollingIntervalSeconds { get; set; } = 10;
}
