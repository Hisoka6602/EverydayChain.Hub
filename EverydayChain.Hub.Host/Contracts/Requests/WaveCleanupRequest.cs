using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 波次清理请求。
/// </summary>
public sealed class WaveCleanupRequest {
    /// <summary>
    /// 波次号。
    /// 可填写范围：长度 1~64；必填。
    /// 空值语义：空字符串或仅空白字符均视为无效请求。
    /// </summary>
    [MaxLength(64)]
    public string WaveCode { get; set; } = string.Empty;
}
