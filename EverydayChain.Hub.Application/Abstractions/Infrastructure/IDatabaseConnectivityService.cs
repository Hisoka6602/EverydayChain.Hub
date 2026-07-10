namespace EverydayChain.Hub.Application.Abstractions.Infrastructure;

/// <summary>
/// 提供数据库连通性探测与异常识别能力。
/// </summary>
public interface IDatabaseConnectivityService
{
    /// <summary>
    /// 获取完整的数据库连通性快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据库连通性快照。</returns>
    Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 强制刷新完整的数据库连通性快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据库连通性快照。</returns>
    Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取本地 MSSQL 的快速连通性状态。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本地 MSSQL 连通性状态。</returns>
    Task<DatabaseEndpointConnectivityState> GetLocalSqlServerStateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 强制刷新本地 MSSQL 的快速连通性状态。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本地 MSSQL 连通性状态。</returns>
    Task<DatabaseEndpointConnectivityState> RefreshLocalSqlServerStateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 判断异常是否属于数据库连接类异常。
    /// </summary>
    /// <param name="exception">待识别异常。</param>
    /// <returns>属于数据库连接类异常时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool IsDatabaseConnectivityException(Exception exception);
}

/// <summary>
/// 表示数据库连通性快照。
/// </summary>
public sealed class DatabaseConnectivitySnapshot
{
    /// <summary>
    /// 获取或设置快照探测时间。
    /// </summary>
    public DateTime CheckedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置本地 MSSQL 连通性状态。
    /// </summary>
    public DatabaseEndpointConnectivityState LocalSqlServer { get; set; } = new();

    /// <summary>
    /// 获取或设置远端 Oracle 连通性状态。
    /// </summary>
    public DatabaseEndpointConnectivityState Oracle { get; set; } = new();

    /// <summary>
    /// 获取当前是否全部数据库均可用。
    /// </summary>
    public bool IsAvailable => LocalSqlServer.IsAvailable && Oracle.IsAvailable;

    /// <summary>
    /// 获取当前是否存在不可用数据库。
    /// </summary>
    public bool HasUnavailableDatabase => !IsAvailable;

    /// <summary>
    /// 构建面向接口调用方的数据库不可用提示。
    /// </summary>
    /// <returns>提示文案。</returns>
    public string BuildUserMessage()
    {
        var messages = new List<string>();
        if (!LocalSqlServer.IsAvailable && !string.IsNullOrWhiteSpace(LocalSqlServer.Description))
        {
            messages.Add(LocalSqlServer.Description.Trim());
        }

        if (!Oracle.IsAvailable && !string.IsNullOrWhiteSpace(Oracle.Description))
        {
            messages.Add(Oracle.Description.Trim());
        }

        if (messages.Count == 0)
        {
            return "数据库连接不可用，请稍后重试。";
        }

        return $"数据库连接不可用：{string.Join("；", messages)}。";
    }
}

/// <summary>
/// 表示单个数据库端点的连通性状态。
/// </summary>
public sealed class DatabaseEndpointConnectivityState
{
    /// <summary>
    /// 获取或设置数据库名称。
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置数据库是否可用。
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 获取或设置当前状态描述。
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
