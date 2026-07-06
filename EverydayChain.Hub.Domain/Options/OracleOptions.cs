namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义当前类型。
/// </summary>
public class OracleOptions
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    public const string SectionName = "Oracle";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string DatabaseMode { get; set; } = "Auto";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool ReadOnly { get; set; } = true;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MaxPageSize { get; set; } = 5000;
}

