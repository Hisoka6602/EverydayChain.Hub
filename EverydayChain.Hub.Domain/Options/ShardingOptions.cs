namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class ShardingOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "Sharding";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string ConnectionString { get; set; } = "Server=localhost,1433;Database=EverydayChainHub;User Id=sa;Password=Your_password123;TrustServerCertificate=true";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int AutoCreateMonthsAhead { get; set; } = 1;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int PreProvisionMaxConcurrency { get; set; } = 4;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool EnableLegacyBaseTableReadFallback { get; set; } = false;
}

