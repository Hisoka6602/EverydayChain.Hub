using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskSeedRequest
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Required(ErrorMessage = "目标表名不能为空。")]
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Required(ErrorMessage = "条码集合不能为空。")]
    public List<string>? Barcodes { get; set; }
}

