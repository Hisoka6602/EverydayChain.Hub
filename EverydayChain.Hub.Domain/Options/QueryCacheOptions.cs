namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 查询缓存配置，从 <c>appsettings.json</c> 的 <c>QueryCache</c> 节点绑定。
/// </summary>
public sealed class QueryCacheOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "QueryCache";

    /// <summary>
    /// 是否启用查询缓存（可填写项：true、false；默认 true）。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 总看板缓存秒数（可填写范围：1~60；默认 2）。
    /// </summary>
    public int GlobalDashboardSeconds { get; set; } = 2;

    /// <summary>
    /// 码头看板缓存秒数（可填写范围：1~60；默认 2）。
    /// </summary>
    public int DockDashboardSeconds { get; set; } = 2;

    /// <summary>
    /// 分拣报表缓存秒数（可填写范围：1~120；默认 10）。
    /// </summary>
    public int SortingReportSeconds { get; set; } = 10;
}
