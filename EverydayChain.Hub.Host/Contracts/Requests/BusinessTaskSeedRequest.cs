using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 业务任务模拟补数请求。
/// </summary>
public sealed class BusinessTaskSeedRequest
{
    /// <summary>
    /// 目标业务任务物理分表名。
    /// 可填写范围：business_tasks_yyyyMM。
    /// 空值语义：为空时请求无效。
    /// </summary>
    [Required(ErrorMessage = "目标表名不能为空。")]
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 待补数条码集合。
    /// 可填写范围：1~5000 项，元素按字符串原值去重。
    /// 空值语义：为空或 null 时请求无效。
    /// </summary>
    [Required(ErrorMessage = "条码集合不能为空。")]
    public List<string>? Barcodes { get; set; }
}
