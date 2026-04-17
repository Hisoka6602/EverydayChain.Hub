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
    /// Oracle 默认目标表名（可填写范围：Oracle 有效表名，仅允许字母、数字、下划线；示例：IDX_SPLIT_TASK；仅在来源类型未识别时回退使用该值）。
    /// </summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>
    /// 拆零来源目标 Schema（可填写范围：Oracle 有效 Schema 名称，仅允许字母、数字、下划线；留空时回退使用 <see cref="Schema"/>）。
    /// </summary>
    public string? SplitSchema { get; set; }

    /// <summary>
    /// 拆零来源目标表名（可填写范围：Oracle 有效表名，仅允许字母、数字、下划线；留空时回退使用 <see cref="Table"/>）。
    /// </summary>
    public string? SplitTable { get; set; }

    /// <summary>
    /// 整件来源目标 Schema（可填写范围：Oracle 有效 Schema 名称，仅允许字母、数字、下划线；留空时回退使用 <see cref="Schema"/>）。
    /// </summary>
    public string? FullCaseSchema { get; set; }

    /// <summary>
    /// 整件来源目标表名（可填写范围：Oracle 有效表名，仅允许字母、数字、下划线；留空时回退使用 <see cref="Table"/>）。
    /// </summary>
    public string? FullCaseTable { get; set; }

    /// <summary>
    /// 默认业务键列名，用于按业务键定位目标行（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；示例：TASK_CODE）。
    /// </summary>
    public string BusinessKeyColumn { get; set; } = string.Empty;

    /// <summary>
    /// 拆零来源业务键列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；留空时回退使用 <see cref="BusinessKeyColumn"/>）。
    /// </summary>
    public string? SplitBusinessKeyColumn { get; set; }

    /// <summary>
    /// 整件来源业务键列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；留空时回退使用 <see cref="BusinessKeyColumn"/>）。
    /// </summary>
    public string? FullCaseBusinessKeyColumn { get; set; }

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
    /// 最后扫描时间回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? ScanTimeColumn { get; set; }

    /// <summary>
    /// 长度回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? LengthColumn { get; set; }

    /// <summary>
    /// 宽度回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? WidthColumn { get; set; }

    /// <summary>
    /// 高度回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? HeightColumn { get; set; }

    /// <summary>
    /// 体积回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? VolumeColumn { get; set; }

    /// <summary>
    /// 重量回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? WeightColumn { get; set; }

    /// <summary>
    /// 扫描次数回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? ScanCountColumn { get; set; }

    /// <summary>
    /// 业务状态回写列名（可填写范围：Oracle 有效列名，仅允许字母、数字、下划线；写入值为本地业务状态枚举整数值；不需要时填写 null 或空字符串）。
    /// </summary>
    public string? BusinessStatusColumn { get; set; }

    /// <summary>
    /// 数据库命令超时秒数（可填写范围：1~3600；建议值 60）。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;
}
