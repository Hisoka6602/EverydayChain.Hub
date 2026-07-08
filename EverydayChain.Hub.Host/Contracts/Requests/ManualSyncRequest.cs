namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示手工同步触发请求参数。
/// </summary>
public sealed class ManualSyncRequest
{
    /// <summary>
    /// 表示同步表或业务表编码。
    /// </summary>
    public string? TableCode { get; set; }
}

