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

    /// <summary>分表基础表名，默认 <c>sorting_task_trace</c>。</summary>
    public string BaseTableName { get; set; } = "sorting_task_trace";

    /// <summary>
    /// 统一纳管的逻辑表名集合（可填写范围：仅允许字母、数字、下划线）。
    /// 优先使用该集合；为空时回退 <see cref="BaseTableName"/> 兼容旧配置。
    /// </summary>
    public HashSet<string> ManagedLogicalTables { get; set; } = [];

    /// <summary>启动时预创建的未来月份数，默认 1。</summary>
    public int AutoCreateMonthsAhead { get; set; } = 1;
}
