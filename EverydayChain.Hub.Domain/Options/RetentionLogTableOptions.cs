namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 日志表保留期配置项。
/// </summary>
public class RetentionLogTableOptions
{
    /// <summary>
    /// 是否启用该日志表保留期治理（可填写项：true、false）。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 日志表逻辑表名（可填写范围：仅字母、数字、下划线；示例：scan_logs）。
    /// </summary>
    public string LogicalTableName { get; set; } = string.Empty;

    /// <summary>
    /// 保留月数（可填写范围：1~120；默认 3）。
    /// </summary>
    public int KeepMonths { get; set; } = 3;

    /// <summary>
    /// 是否启用 dry-run（可填写项：true、false；true 时仅输出审计日志不执行删除）。
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// 是否允许执行危险删除动作（可填写项：true、false）。
    /// </summary>
    public bool AllowDrop { get; set; }
}
