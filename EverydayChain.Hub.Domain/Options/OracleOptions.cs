namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 远端 Oracle 只读连接配置，从 <c>appsettings.json</c> 的 <c>Oracle</c> 节点绑定。
/// </summary>
public class OracleOptions
{
    /// <summary>配置节名称。</summary>
    public const string SectionName = "Oracle";

    /// <summary>远端 Oracle 连接字符串（Oracle 标准连接字符串，建议启用连接池并通过密钥注入密码）。</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>默认源端 Schema（Oracle 有效 Schema 名称）。</summary>
    public string DefaultSchema { get; set; } = string.Empty;

    /// <summary>是否强制只读（true 表示所有命令统一设置为只读）。</summary>
    public bool ReadOnly { get; set; } = true;

    /// <summary>数据库命令超时秒数（建议范围：1~3600）。</summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>分页读取允许的最大 PageSize（建议范围：1~100000）。</summary>
    public int MaxPageSize { get; set; } = 5000;
}
