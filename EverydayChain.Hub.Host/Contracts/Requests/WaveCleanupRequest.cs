using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class WaveCleanupRequest {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string WaveCode { get; set; } = string.Empty;
}

