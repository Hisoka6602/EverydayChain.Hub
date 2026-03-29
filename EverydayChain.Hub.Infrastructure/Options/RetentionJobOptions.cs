namespace EverydayChain.Hub.Infrastructure.Options;

/// <summary>
/// 分表保留期后台任务配置，从 <c>appsettings.json</c> 的 <c>RetentionJob</c> 节点绑定。
/// </summary>
public class RetentionJobOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "RetentionJob";

    /// <summary>是否启用保留期后台任务总开关。</summary>
    public bool Enabled { get; set; }

    /// <summary>保留期后台任务轮询间隔（秒），默认 3600 秒。</summary>
    public int PollingIntervalSeconds { get; set; } = 3600;

    /// <summary>保留期动作总开关，关闭后仅记录跳过日志。</summary>
    public bool AllowDangerousAction { get; set; }
}
