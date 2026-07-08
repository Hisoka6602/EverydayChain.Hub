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
    /// 存储连通性快照缓存秒数。
    /// </summary>
    private const int ProbeCacheSeconds = 5;

    /// <summary>
    /// 存储单次探测超时秒数。
    /// </summary>
    private const int ProbeTimeoutSeconds = 5;

    /// <summary>
    /// 存储本地数据库名称。
    /// </summary>
    private const string LocalSqlServerName = "本地 MSSQL";

    /// <summary>
    /// 存储远端数据库名称。
    /// </summary>
    private const string OracleName = "远端 Oracle";

    /// <summary>
    /// 存储 Oracle 配置。
    /// </summary>
    private readonly OracleOptions _oracleOptions = oracleOptions.Value;

    /// <summary>
    /// 存储探测互斥锁。
    /// </summary>
    private readonly SemaphoreSlim _probeLock = new(1, 1);

    /// <summary>
    /// 存储最近一次快照。
    /// </summary>
    private DatabaseConnectivitySnapshot _lastSnapshot = new()
    {
        CheckedAtLocal = DateTime.MinValue,
        LocalSqlServer = new DatabaseEndpointConnectivityState
        {
            DatabaseName = LocalSqlServerName,
            IsAvailable = false,
            Description = $"{LocalSqlServerName} 尚未完成连通性探测"
        },
        Oracle = new DatabaseEndpointConnectivityState
        {
            DatabaseName = OracleName,
            IsAvailable = false,
            Description = $"{OracleName} 尚未完成连通性探测"
        }
    };

    /// <summary>
    /// 存储最近一次探测时间戳。
    /// </summary>
    private long _lastProbeTimestamp;

    /// <summary>
    /// 存储最近一次已记录日志的快照指纹。
    /// </summary>
    private string _lastLoggedFingerprint = string.Empty;

    /// <summary>
    /// 获取数据库连通性快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据库连通性快照。</returns>
    public async Task<DatabaseConnectivitySnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        if (CanReuseCachedSnapshot())
        {
            return _lastSnapshot;
        }

        return await RefreshSnapshotAsync(cancellationToken);
    }

    /// <summary>
    /// 强制刷新数据库连通性快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据库连通性快照。</returns>
    public async Task<DatabaseConnectivitySnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
    {
        await _probeLock.WaitAsync(cancellationToken);
        try
        {
            // 步骤：Refresh 语义必须绕过缓存，确保自动迁移后的可用性状态能够被立即重新探测。
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
    /// 判断当前是否可以复用缓存快照。
    /// </summary>
    /// <returns>可复用时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private bool CanReuseCachedSnapshot()
    {
        var lastProbeTimestamp = Interlocked.Read(ref _lastProbeTimestamp);
        if (lastProbeTimestamp == 0)
        {
            return false;
        }

        var cacheWindowTicks = ProbeCacheSeconds * Stopwatch.Frequency;
        var elapsedTicks = Stopwatch.GetTimestamp() - lastProbeTimestamp;
        return elapsedTicks <= cacheWindowTicks;
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
    /// 更新缓存快照并按状态变化输出日志。
    /// </summary>
    /// <param name="snapshot">最新快照。</param>
    private void UpdateCachedSnapshot(DatabaseConnectivitySnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        Interlocked.Exchange(ref _lastProbeTimestamp, Stopwatch.GetTimestamp());
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
    /// 构建快照状态指纹。
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
