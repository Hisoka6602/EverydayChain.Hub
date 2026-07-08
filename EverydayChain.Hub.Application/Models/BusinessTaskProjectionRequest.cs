namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskProjectionRequest 类型。
/// </summary>
public class BusinessTaskProjectionRequest
{
    /// <summary>
    /// 获取或设置 Rows。
    /// </summary>
    public IReadOnlyList<BusinessTaskProjectionRow> Rows { get; set; } = [];
}

