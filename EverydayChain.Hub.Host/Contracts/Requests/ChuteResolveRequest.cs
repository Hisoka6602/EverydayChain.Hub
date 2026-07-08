using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示格口解析请求参数。
/// </summary>
public sealed class ChuteResolveRequest {
    /// <summary>
    /// 表示待解析条码关联的业务任务编码。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示需要解析目标格口的业务条码。
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;
}

