namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 ShardingOptions 类型。
/// </summary>
public class ShardingOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "Sharding";

    /// <summary>
    /// 获取或设置 ConnectionString。
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Schema。
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// 获取或设置 AutoCreateMonthsAhead。
    /// </summary>
    public int AutoCreateMonthsAhead { get; set; } = 1;

    /// <summary>
    /// 获取或设置 PreProvisionMaxConcurrency。
    /// </summary>
    public int PreProvisionMaxConcurrency { get; set; } = 4;

    /// <summary>
    /// 获取或设置 EnableLegacyBaseTableReadFallback。
    /// </summary>
    public bool EnableLegacyBaseTableReadFallback { get; set; } = false;
}

