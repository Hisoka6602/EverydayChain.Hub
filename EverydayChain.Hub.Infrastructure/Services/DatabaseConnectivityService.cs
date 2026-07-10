using System.Data;
using System.Data.Common;
using System.Diagnostics;
using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 提供数据库连通性探测服务。
/// </summary>
public sealed class DatabaseConnectivityService(
    IDbContextFactory<HubDbContext> dbContextFactory,
    IOptions<OracleOptions> oracleOptions,
    ILogger<DatabaseConnectivityService> logger) : IDatabaseConnectivityService
{
    /// <summary>
    /// 快照缓存秒数。
    /// </summary>
    private const int ProbeCacheSeconds = 5;

    /// <summary>
    /// 单次探测超时秒数。
    /// </summary>
    private const int ProbeTimeoutSeconds = 5;

    /// <summary>
    /// 本地数据库名称。
    /// </summary>
    private const string LocalSqlServerName = "本地 MSSQL";

    /// <summary>
    /// 远端数据库名称。
    /// </summary>
    private const string OracleName = "远端 Oracle";

    /// <summary>
    /// 存储 Oracle 配置。
    /// </summary>
    private readonly OracleOptions _oracleOptions = oracleOptions.Value;

    /// <summary>
    /// 完整快照探测锁，避免重复串行探测 Oracle。
    /// </summary>
    private readonly SemaphoreSlim _probeLock = new(1, 1);

    /// <summary>
    /// 本地 MSSQL 快速探测锁，避免并发重复打本地库。
    /// </summary>
    private readonly SemaphoreSlim _localSqlProbeLock = new(1, 1);

    /// <summary>
    /// 最近一次完整快照。
    /// </summary>
    private DatabaseConnectivitySnapshot _lastSnapshot = CreateUnknownSnapshot();

    /// <summary>
    /// 最近一次完整探测时间戳。
    /// </summary>
    private long _lastProbeTimestamp;

    /// <summary>
    /// 最近一次完整快照的日志指纹。
    /// </summary>
    private string _lastLoggedFingerprint = string.Empty;

    /// <summary>
    /// 最近一次本地 MSSQL 连通性状态。
    /// </summary>
    private DatabaseEndpointConnectivityState _lastLocalSqlServerState = CreateUnknownState(LocalSqlServerName);

    /// <summary>
    /// 最近一次本地 MSSQL 探测时间戳。
    /// </summary>
    private long _lastLocalSqlProbeTimestamp;

    /// <summary>
    /// 最近一次本地 MSSQL 状态日志指纹。
    /// </summary>
    private string _lastLoggedLocalSqlFingerprint = string.Empty;

    /// <summary>
    /// 获取完整的数据库连通性快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据库连通性快照。</returns>
    public async Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (CanReuseCachedProbe(Interlocked.Read(ref _lastProbeTimestamp)))
        {
            return _lastSnapshot;
        }

        return await RefreshSnapshotAsync(cancellationToken);
    }

    /// <summary>
    /// 强制刷新完整的数据库连通性快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据库连通性快照。</returns>
    public async Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
    {
        await _probeLock.WaitAsync(cancellationToken);
        try
        {
            var snapshot = await ProbeSnapshotAsync(cancellationToken);
            UpdateCachedSnapshot(snapshot);
            return snapshot;
        }
        finally
        {
            _probeLock.Release();
        }
    }

    /// <summary>
    /// 获取本地 MSSQL 的快速连通性状态。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本地 MSSQL 连通性状态。</returns>
    public async Task<DatabaseEndpointConnectivityState> GetLocalSqlServerStateAsync(CancellationToken cancellationToken)
    {
        if (CanReuseCachedProbe(Interlocked.Read(ref _lastLocalSqlProbeTimestamp)))
        {
            return _lastLocalSqlServerState;
        }

        return await RefreshLocalSqlServerStateAsync(cancellationToken);
    }

    /// <summary>
    /// 强制刷新本地 MSSQL 的快速连通性状态。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本地 MSSQL 连通性状态。</returns>
    public async Task<DatabaseEndpointConnectivityState> RefreshLocalSqlServerStateAsync(CancellationToken cancellationToken)
    {
        await _localSqlProbeLock.WaitAsync(cancellationToken);
        try
        {
            var checkedAtLocal = DateTime.Now;
            var localSqlServerState = await ProbeLocalSqlServerAsync(cancellationToken);
            UpdateCachedLocalSqlServerState(localSqlServerState, checkedAtLocal, shouldLog: true);
            return localSqlServerState;
        }
        finally
        {
            _localSqlProbeLock.Release();
        }
    }

    /// <summary>
    /// 判断异常是否属于数据库连接类异常。
    /// </summary>
    /// <param name="exception">待识别异常。</param>
    /// <returns>属于数据库连接类异常时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public bool IsDatabaseConnectivityException(Exception exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (current is SqlException sqlException && IsSqlConnectivityException(sqlException))
            {
                return true;
            }

            if (current is OracleException oracleException && IsOracleConnectivityException(oracleException))
            {
                return true;
            }

            if (current is DbException dbException && IsDbConnectivityException(dbException))
            {
                return true;
            }

            if (current is RetryLimitExceededException retryLimitExceededException
                && retryLimitExceededException.InnerException is not null
                && IsDatabaseConnectivityException(retryLimitExceededException.InnerException))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    /// <summary>
    /// 执行完整快照探测。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据库连通性快照。</returns>
    private async Task<DatabaseConnectivitySnapshot> ProbeSnapshotAsync(CancellationToken cancellationToken)
    {
        var checkedAtLocal = DateTime.Now;
        var localSqlServer = await ProbeLocalSqlServerAsync(cancellationToken);
        var oracle = await ProbeOracleAsync(cancellationToken);
        return new DatabaseConnectivitySnapshot
        {
            CheckedAtLocal = checkedAtLocal,
            LocalSqlServer = localSqlServer,
            Oracle = oracle
        };
    }

    /// <summary>
    /// 探测本地 MSSQL 连通性。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本地 MSSQL 连通性状态。</returns>
    private async Task<DatabaseEndpointConnectivityState> ProbeLocalSqlServerAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(ProbeTimeoutSeconds));
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(timeoutCts.Token);
            var connection = dbContext.Database.GetDbConnection();
            var shouldClose = connection.State == ConnectionState.Closed;
            if (shouldClose)
            {
                await connection.OpenAsync(timeoutCts.Token);
            }

            if (shouldClose)
            {
                await connection.CloseAsync();
            }

            return CreateAvailableState(LocalSqlServerName);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return CreateUnavailableState(LocalSqlServerName, "连接探测超时");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateUnavailableState(LocalSqlServerName, ExtractReadableMessage(ex));
        }
    }

    /// <summary>
    /// 探测远端 Oracle 连通性。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>远端 Oracle 连通性状态。</returns>
    private async Task<DatabaseEndpointConnectivityState> ProbeOracleAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(ProbeTimeoutSeconds));
        try
        {
            var connectionString = OracleConnectionStringResolver.BuildEffectiveConnectionString(_oracleOptions);
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync(timeoutCts.Token);
            await connection.CloseAsync();
            return CreateAvailableState(OracleName);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return CreateUnavailableState(OracleName, "连接探测超时");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateUnavailableState(OracleName, ExtractReadableMessage(ex));
        }
    }

    /// <summary>
    /// 更新完整快照缓存并输出状态变化日志。
    /// </summary>
    /// <param name="snapshot">最新快照。</param>
    private void UpdateCachedSnapshot(DatabaseConnectivitySnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        Interlocked.Exchange(ref _lastProbeTimestamp, Stopwatch.GetTimestamp());
        UpdateCachedLocalSqlServerState(snapshot.LocalSqlServer, snapshot.CheckedAtLocal, shouldLog: false);

        var fingerprint = BuildSnapshotFingerprint(snapshot);
        if (string.Equals(fingerprint, _lastLoggedFingerprint, StringComparison.Ordinal))
        {
            return;
        }

        _lastLoggedFingerprint = fingerprint;
        if (snapshot.IsAvailable)
        {
            logger.LogInformation(
                "数据库连通性探测通过。CheckedAtLocal={CheckedAtLocal}",
                snapshot.CheckedAtLocal);
            return;
        }

        logger.LogWarning(
            "数据库连通性探测发现不可用数据库。CheckedAtLocal={CheckedAtLocal}, Message={Message}",
            snapshot.CheckedAtLocal,
            snapshot.BuildUserMessage());
    }

    /// <summary>
    /// 更新本地 MSSQL 快速探测缓存并按需输出状态变化日志。
    /// </summary>
    /// <param name="localSqlServerState">最新本地 MSSQL 状态。</param>
    /// <param name="checkedAtLocal">探测时间。</param>
    /// <param name="shouldLog">是否输出日志。</param>
    private void UpdateCachedLocalSqlServerState(
        DatabaseEndpointConnectivityState localSqlServerState,
        DateTime checkedAtLocal,
        bool shouldLog)
    {
        _lastLocalSqlServerState = CloneState(localSqlServerState);
        Interlocked.Exchange(ref _lastLocalSqlProbeTimestamp, Stopwatch.GetTimestamp());

        var fingerprint = BuildLocalSqlServerFingerprint(localSqlServerState);
        if (string.Equals(fingerprint, _lastLoggedLocalSqlFingerprint, StringComparison.Ordinal))
        {
            return;
        }

        _lastLoggedLocalSqlFingerprint = fingerprint;
        if (!shouldLog)
        {
            return;
        }

        if (localSqlServerState.IsAvailable)
        {
            logger.LogInformation(
                "本地 MSSQL 快速探测通过。CheckedAtLocal={CheckedAtLocal}",
                checkedAtLocal);
            return;
        }

        logger.LogWarning(
            "本地 MSSQL 快速探测失败。CheckedAtLocal={CheckedAtLocal}, Description={Description}",
            checkedAtLocal,
            localSqlServerState.Description);
    }

    /// <summary>
    /// 判断探测缓存是否仍可复用。
    /// </summary>
    /// <param name="lastProbeTimestamp">最近一次探测时间戳。</param>
    /// <returns>可复用时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private static bool CanReuseCachedProbe(long lastProbeTimestamp)
    {
        if (lastProbeTimestamp == 0)
        {
            return false;
        }

        var cacheWindowTicks = ProbeCacheSeconds * Stopwatch.Frequency;
        var elapsedTicks = Stopwatch.GetTimestamp() - lastProbeTimestamp;
        return elapsedTicks <= cacheWindowTicks;
    }

    /// <summary>
    /// 构建完整快照状态指纹。
    /// </summary>
    /// <param name="snapshot">数据库连通性快照。</param>
    /// <returns>状态指纹。</returns>
    private static string BuildSnapshotFingerprint(DatabaseConnectivitySnapshot snapshot)
    {
        return string.Join(
            "|",
            snapshot.LocalSqlServer.IsAvailable,
            snapshot.LocalSqlServer.Description,
            snapshot.Oracle.IsAvailable,
            snapshot.Oracle.Description);
    }

    /// <summary>
    /// 构建本地 MSSQL 状态指纹。
    /// </summary>
    /// <param name="localSqlServerState">本地 MSSQL 状态。</param>
    /// <returns>状态指纹。</returns>
    private static string BuildLocalSqlServerFingerprint(DatabaseEndpointConnectivityState localSqlServerState)
    {
        return string.Join(
            "|",
            localSqlServerState.IsAvailable,
            localSqlServerState.Description);
    }

    /// <summary>
    /// 创建完整未知状态快照。
    /// </summary>
    /// <returns>未知状态快照。</returns>
    private static DatabaseConnectivitySnapshot CreateUnknownSnapshot()
    {
        return new DatabaseConnectivitySnapshot
        {
            CheckedAtLocal = DateTime.MinValue,
            LocalSqlServer = CreateUnknownState(LocalSqlServerName),
            Oracle = CreateUnknownState(OracleName)
        };
    }

    /// <summary>
    /// 创建可用状态对象。
    /// </summary>
    /// <param name="databaseName">数据库名称。</param>
    /// <returns>数据库端点状态。</returns>
    private static DatabaseEndpointConnectivityState CreateAvailableState(string databaseName)
    {
        return new DatabaseEndpointConnectivityState
        {
            DatabaseName = databaseName,
            IsAvailable = true,
            Description = $"{databaseName} 连接正常"
        };
    }

    /// <summary>
    /// 创建未知状态对象。
    /// </summary>
    /// <param name="databaseName">数据库名称。</param>
    /// <returns>数据库端点状态。</returns>
    private static DatabaseEndpointConnectivityState CreateUnknownState(string databaseName)
    {
        return new DatabaseEndpointConnectivityState
        {
            DatabaseName = databaseName,
            IsAvailable = false,
            Description = $"{databaseName} 尚未完成连通性探测"
        };
    }

    /// <summary>
    /// 创建不可用状态对象。
    /// </summary>
    /// <param name="databaseName">数据库名称。</param>
    /// <param name="detail">失败详情。</param>
    /// <returns>数据库端点状态。</returns>
    private static DatabaseEndpointConnectivityState CreateUnavailableState(string databaseName, string detail)
    {
        var normalizedDetail = string.IsNullOrWhiteSpace(detail) ? "未知错误" : detail.Trim();
        return new DatabaseEndpointConnectivityState
        {
            DatabaseName = databaseName,
            IsAvailable = false,
            Description = $"{databaseName} 无法连接（{normalizedDetail}）"
        };
    }

    /// <summary>
    /// 克隆数据库端点状态，避免外部修改缓存对象。
    /// </summary>
    /// <param name="state">待克隆状态。</param>
    /// <returns>克隆后的状态对象。</returns>
    private static DatabaseEndpointConnectivityState CloneState(DatabaseEndpointConnectivityState state)
    {
        return new DatabaseEndpointConnectivityState
        {
            DatabaseName = state.DatabaseName,
            IsAvailable = state.IsAvailable,
            Description = state.Description
        };
    }

    /// <summary>
    /// 判断 SqlException 是否属于连接类异常。
    /// </summary>
    /// <param name="exception">SqlException 实例。</param>
    /// <returns>属于连接类异常时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private static bool IsSqlConnectivityException(SqlException exception)
    {
        if (exception.IsTransient)
        {
            return true;
        }

        foreach (SqlError error in exception.Errors)
        {
            switch (error.Number)
            {
                case -2:
                case 2:
                case 53:
                case 233:
                case 258:
                case 4060:
                case 18456:
                case 40613:
                case 10053:
                case 10054:
                case 10060:
                case 10061:
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断 OracleException 是否属于连接类异常。
    /// </summary>
    /// <param name="exception">OracleException 实例。</param>
    /// <returns>属于连接类异常时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private static bool IsOracleConnectivityException(OracleException exception)
    {
        return exception.Number switch
        {
            1017 => true,
            12154 => true,
            12170 => true,
            12514 => true,
            12516 => true,
            12518 => true,
            12519 => true,
            12520 => true,
            12521 => true,
            12537 => true,
            12541 => true,
            12543 => true,
            12545 => true,
            12547 => true,
            12560 => true,
            12564 => true,
            12571 => true,
            _ => false
        };
    }

    /// <summary>
    /// 判断通用 DbException 是否属于连接类异常。
    /// </summary>
    /// <param name="exception">DbException 实例。</param>
    /// <returns>属于连接类异常时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private static bool IsDbConnectivityException(DbException exception)
    {
        if (exception is SqlException sqlException)
        {
            return IsSqlConnectivityException(sqlException);
        }

        if (exception is OracleException oracleException)
        {
            return IsOracleConnectivityException(oracleException);
        }

        if (exception.IsTransient)
        {
            return true;
        }

        var message = exception.Message;
        return message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("network", StringComparison.OrdinalIgnoreCase)
            || message.Contains("连接", StringComparison.OrdinalIgnoreCase)
            || message.Contains("listener", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 提取适合日志与接口输出的异常描述。
    /// </summary>
    /// <param name="exception">异常对象。</param>
    /// <returns>单行异常描述。</returns>
    private static string ExtractReadableMessage(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        var message = string.IsNullOrWhiteSpace(current.Message) ? exception.Message : current.Message;
        return message
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }
}
