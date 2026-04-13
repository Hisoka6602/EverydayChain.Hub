using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 请求格口入参。
/// </summary>
public sealed class ChuteResolveRequest {
    /// <summary>
    /// 业务任务编码，长度范围 0~64。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本，长度范围 1~128。
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;
}
