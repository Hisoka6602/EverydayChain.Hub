using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 波次清理请求。
/// </summary>
public sealed class WaveCleanupRequest {
    /// <summary>
    /// 波次号（可填写范围：1~64 个字符）。
    /// </summary>
    [MaxLength(64)]
    public string WaveCode { get; set; } = string.Empty;
}
