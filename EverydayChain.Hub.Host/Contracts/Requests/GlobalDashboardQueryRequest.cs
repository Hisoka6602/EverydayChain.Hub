namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 总看板查询请求。
/// </summary>
public sealed class GlobalDashboardQueryRequest
{
    /// <summary>
    /// 查询开始时间（本地时间，包含边界）。
    /// 可填写范围：必须大于 <see cref="DateTime.MinValue"/>；必填。
    /// 空值语义：该字段为值类型，未传时会触发时间范围校验失败。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 查询结束时间（本地时间，不包含边界）。
    /// 可填写范围：必须大于 <see cref="StartTimeLocal"/>；必填。
    /// 空值语义：该字段为值类型，未传时会触发时间范围校验失败。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }
}
