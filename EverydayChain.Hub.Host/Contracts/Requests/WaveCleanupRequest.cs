using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示波次清理请求参数。
/// </summary>
public sealed class WaveCleanupRequest {
    /// <summary>
    /// 表示需要执行清理的目标波次号。
    /// </summary>
    [MaxLength(64)]
    public string WaveCode { get; set; } = string.Empty;
}

