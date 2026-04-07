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

    /// <summary>
    /// 连接库名（可填写 Oracle ServiceName 或 SID；为空时按 ConnectionString 原样连接）。
    /// 注意：该值用于“连到哪个 Oracle 实例/服务”，不是 Schema 名。
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// 连接库名模式（可选值：ServiceName、Sid、Auto）。
    /// Auto：对 host:port 优先按 ServiceName（/）拼接；Sid：强制按 SID（:）拼接。
    /// </summary>
    public string DatabaseMode { get; set; } = "Auto";

    /// <summary>
    /// 默认源端 Schema（Oracle 有效 Schema 名称）。
    /// 仅用于拼接查询对象名（如 <c>SCHEMA.TABLE</c>），不参与 Oracle 网络连接目标选择。
    /// 因此不能替代 <see cref="Database"/>。
    /// </summary>
    public string DefaultSchema { get; set; } = string.Empty;

    /// <summary>是否强制只读（true 表示仅允许执行 SELECT 语句）。</summary>
    public bool ReadOnly { get; set; } = true;

    /// <summary>数据库命令超时秒数（建议范围：1~3600）。</summary>
    public int CommandTimeoutSeconds { get; set; } = 60;

    /// <summary>分页读取允许的最大 PageSize（建议范围：1~100000）。</summary>
    public int MaxPageSize { get; set; } = 5000;
}
