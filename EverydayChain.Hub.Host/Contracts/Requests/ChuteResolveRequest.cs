using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ChuteResolveRequest {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;
}

