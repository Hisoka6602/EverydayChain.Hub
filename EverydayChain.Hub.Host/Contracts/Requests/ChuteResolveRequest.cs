using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 请求格口入参。
/// </summary>
public sealed class ChuteResolveRequest {
    /// <summary>
    /// 业务任务编码。
    /// 可填写范围：长度 0~64；可选。
    /// 空值语义：为空时仅按 <see cref="Barcode"/> 匹配目标任务。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本。
    /// 可填写范围：长度 1~128；必填。
    /// 空值语义：空字符串或仅空白字符均视为无效请求。
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Barcode { get; set; } = string.Empty;
}
