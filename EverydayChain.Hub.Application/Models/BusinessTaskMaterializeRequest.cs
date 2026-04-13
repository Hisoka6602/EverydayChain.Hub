namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务物化输入模型，承载从同步链路传入的最小字段。
/// </summary>
public class BusinessTaskMaterializeRequest
{
    /// <summary>
    /// 任务编码（可填写范围：1~64 个字符，不能为空白）。
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 来源同步表编码（可填写范围：1~64 个字符，不能为空白）。
    /// </summary>
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 业务键文本（可填写范围：1~256 个字符，不能为空白）。
    /// </summary>
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本（可填写范围：1~128 个字符，可为空）。
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// 物化时间（本地时间）；为空时将使用当前本地时间。
    /// </summary>
    public DateTime? MaterializedTimeLocal { get; set; }
}
