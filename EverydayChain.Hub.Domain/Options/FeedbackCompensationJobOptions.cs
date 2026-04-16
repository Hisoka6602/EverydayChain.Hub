namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 业务回传补偿后台任务配置，从 <c>appsettings.json</c> 的 <c>FeedbackCompensationJob</c> 节点绑定。
/// </summary>
public class FeedbackCompensationJobOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "FeedbackCompensationJob";

    /// <summary>
    /// 是否启用业务回传补偿后台任务（可填写项：true、false；默认 false）。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 任务轮询间隔（单位：秒；可填写范围：1~86400；默认 300）。
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// 每轮最大补偿任务数（可填写范围：1~1000；默认 100）。
    /// </summary>
    public int BatchSize { get; set; } = 100;
}
