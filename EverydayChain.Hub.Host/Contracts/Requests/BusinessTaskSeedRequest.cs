using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示业务任务手工补数请求参数。
/// </summary>
public sealed class BusinessTaskSeedRequest
{
    /// <summary>
    /// 表示补数写入的目标表名。
    /// </summary>
    [Required(ErrorMessage = "目标表名不能为空。")]
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 表示需要补写到本地库的条码列表。
    /// </summary>
    [Required(ErrorMessage = "条码集合不能为空。")]
    public List<string>? Barcodes { get; set; }
}

