namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 分表配置，从 <c>appsettings.json</c> 的 <c>Sharding</c> 节点绑定。
/// </summary>
public class ShardingOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "Sharding";

    /// <summary>SQL Server 连接字符串。</summary>
    public string ConnectionString { get; set; } = "Server=localhost,1433;Database=EverydayChainHub;User Id=sa;Password=Your_password123;TrustServerCertificate=true";

    /// <summary>目标 Schema，默认 <c>dbo</c>。</summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>启动时预创建的未来月份数，默认 1。</summary>
    public int AutoCreateMonthsAhead { get; set; } = 1;

    /// <summary>分表预建并发上限，范围 1-64，默认 4。</summary>
    public int PreProvisionMaxConcurrency { get; set; } = 4;

    /// <summary>
    /// 是否启用无后缀历史表读取兜底（可填写项：true、false；默认 false）。
    /// 当环境已完成迁移且无历史无后缀表时，可配置为 false 以减少无效查询往返。
    /// </summary>
    public bool EnableLegacyBaseTableReadFallback { get; set; } = false;
}
