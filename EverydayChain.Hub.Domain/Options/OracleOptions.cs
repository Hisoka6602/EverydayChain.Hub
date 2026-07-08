namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 OracleOptions 类型。
/// </summary>
public class OracleOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "Oracle";

    /// <summary>
    /// 获取或设置 ConnectionString。
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Database。
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 DatabaseMode。
    /// </summary>
    public string DatabaseMode { get; set; } = "Auto";

    /// <summary>
    /// 获取或设置 ReadOnly。
    /// </summary>
    public bool ReadOnly { get; set; } = true;

    /// <summary>
    /// 获取或设置 CommandTimeoutSeconds。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置 MaxPageSize。
    /// </summary>
    public int MaxPageSize { get; set; } = 5000;
}

