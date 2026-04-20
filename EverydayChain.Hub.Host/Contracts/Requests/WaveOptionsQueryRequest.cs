namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 波次选项查询请求。
/// </summary>
public sealed class WaveOptionsQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含边界）。
    /// 可填写范围：必须传入本地时间语义。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含边界）。
    /// 可填写范围：必须传入本地时间语义，且大于开始时间。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }
}
