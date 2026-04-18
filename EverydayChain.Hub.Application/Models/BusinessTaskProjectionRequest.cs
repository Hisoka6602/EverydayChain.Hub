namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务投影请求。
/// </summary>
public class BusinessTaskProjectionRequest
{
    /// <summary>
    /// 待投影行集合。
    /// </summary>
    public IReadOnlyList<BusinessTaskProjectionRow> Rows { get; set; } = [];
}
