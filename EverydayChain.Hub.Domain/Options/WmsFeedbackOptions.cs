namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 业务回传 Oracle 写入配置，从 <c>appsettings.json</c> 的 <c>WmsFeedback</c> 节点绑定。
/// </summary>
public class WmsFeedbackOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "WmsFeedback";

    /// <summary>
    /// 是否启用业务回传写入（可填写项：true、false；默认 false 表示仅记录本地日志不回写 Oracle）。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Oracle 目标 Schema（可填写范围：Oracle 有效 Schema 名称，仅允许字母、数字、下划线；示例：WMS_USER_431）。
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Oracle 目标表名（可填写范围：Oracle 有效表名，仅允许字母、数字、下划线；示例：IDX_FEEDBACK_TABLE）。
    /// </summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>
    /// 业务键列名，用于按业务键定位目标行（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；示例：TASK_CODE）。
    /// </summary>
    public string BusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 回传状态列名，写入回传完成标志（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；示例：FEEDBACK_STATUS）。
    /// </summary>
    public string FeedbackStatusColumn { get; set; } = string.Empty;

    /// <summary>
    /// 回传完成状态值（可填写范围：任意非空字符串；示例：Y）。
    /// </summary>
    public string FeedbackCompletedValue { get; set; } = "Y";

    /// <summary>
    /// 回传时间审计列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? FeedbackTimeColumn { get; set; }

    /// <summary>
    /// 格口编码回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? ActualChuteColumn { get; set; }

    /// <summary>
    /// 数据库命令超时秒数（可填写范围：1~3600；建议值 60）。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;
}
