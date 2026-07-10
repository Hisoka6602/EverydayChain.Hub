using System.Diagnostics;
using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Domain.Runtime;
using EverydayChain.Hub.Infrastructure.Persistence;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义 RuntimeLeaseRepository 类型。
/// </summary>
public sealed class RuntimeLeaseRepository(
    IDbContextFactory<HubDbContext> dbContextFactory,
    ILogger<RuntimeLeaseRepository> logger) : IRuntimeLeaseRepository
{
    /// <summary>
    /// 尝试获取指定租约。
    /// </summary>
    /// <param name="leaseKey">租约键。</param>
    /// <param name="ownerId">持有者标识。</param>
    /// <param name="acquiredTimeLocal">获取时间。</param>
    /// <param name="expiresAtLocal">过期时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>获取成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public async Task<bool> TryAcquireAsync(
        string leaseKey,
        string ownerId,
        DateTime acquiredTimeLocal,
        DateTime expiresAtLocal,
        CancellationToken ct)
    {
        // 步骤：先尝试覆盖已过期租约或插入新租约；若失败，再识别是否属于同机失活进程遗留锁并尝试接管。
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var connection = (SqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var acquired = await TryAcquireOrReplaceExpiredLeaseAsync(
            connection,
            leaseKey,
            ownerId,
            acquiredTimeLocal,
            expiresAtLocal,
            ct);
        if (acquired)
        {
            return true;
        }

        var activeLease = await LoadLeaseSnapshotAsync(connection, leaseKey, ct);
        if (activeLease is null || !ShouldRecoverAbandonedLease(activeLease.OwnerId))
        {
            return false;
        }

        logger.LogWarning(
            "检测到同机失活进程遗留的运行时租约，准备尝试接管。LeaseKey={LeaseKey}, PreviousOwnerId={PreviousOwnerId}, PreviousExpiresAtLocal={PreviousExpiresAtLocal}",
            leaseKey,
            activeLease.OwnerId,
            activeLease.ExpiresAtLocal);
        var takeoverSucceeded = await TryTakeOverAbandonedLeaseAsync(
            connection,
            activeLease,
            ownerId,
            acquiredTimeLocal,
            expiresAtLocal,
            ct);
        if (takeoverSucceeded)
        {
            logger.LogWarning(
                "已接管同机失活进程遗留的运行时租约。LeaseKey={LeaseKey}, PreviousOwnerId={PreviousOwnerId}, NewOwnerId={NewOwnerId}",
                leaseKey,
                activeLease.OwnerId,
                ownerId);
        }

        return takeoverSucceeded;
    }

    /// <summary>
    /// 读取指定租约当前快照。
    /// </summary>
    /// <param name="leaseKey">租约键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>租约存在时返回快照；否则返回 <see langword="null"/>。</returns>
    public async Task<RuntimeLeaseSnapshot?> GetAsync(string leaseKey, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseKey);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.RuntimeLeases
            .AsNoTracking()
            .Where(lease => lease.Id == leaseKey)
            .Select(lease => new RuntimeLeaseSnapshot
            {
                LeaseKey = lease.Id,
                OwnerId = lease.OwnerId,
                AcquiredTimeLocal = lease.AcquiredTimeLocal,
                ExpiresAtLocal = lease.ExpiresAtLocal
            })
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// 释放指定租约。
    /// </summary>
    /// <param name="leaseKey">租约键。</param>
    /// <param name="ownerId">持有者标识。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task ReleaseAsync(string leaseKey, string ownerId, CancellationToken ct)
    {
        // 步骤：仅删除当前实例自己持有的租约记录，避免误删其他实例的租约。
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        await dbContext.RuntimeLeases
            .Where(lease => lease.Id == leaseKey && lease.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// 执行首次抢占或覆盖已过期租约。
    /// </summary>
    /// <param name="connection">已打开的 SQL 连接。</param>
    /// <param name="leaseKey">租约键。</param>
    /// <param name="ownerId">新持有者标识。</param>
    /// <param name="acquiredTimeLocal">获取时间。</param>
    /// <param name="expiresAtLocal">过期时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>抢占成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    private static async Task<bool> TryAcquireOrReplaceExpiredLeaseAsync(
        SqlConnection connection,
        string leaseKey,
        string ownerId,
        DateTime acquiredTimeLocal,
        DateTime expiresAtLocal,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE [dbo].[runtime_leases]
            SET [OwnerId] = @ownerId,
                [AcquiredTimeLocal] = @acquiredTimeLocal,
                [ExpiresAtLocal] = @expiresAtLocal
            WHERE [Id] = @leaseKey
              AND [ExpiresAtLocal] <= @acquiredTimeLocal;

            IF @@ROWCOUNT = 0
            BEGIN
                BEGIN TRY
                    INSERT INTO [dbo].[runtime_leases] ([Id], [OwnerId], [AcquiredTimeLocal], [ExpiresAtLocal])
                    VALUES (@leaseKey, @ownerId, @acquiredTimeLocal, @expiresAtLocal);
                END TRY
                BEGIN CATCH
                    IF ERROR_NUMBER() NOT IN (2601, 2627)
                    BEGIN
                        THROW;
                    END
                END CATCH
            END

            SELECT
                CAST(CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM [dbo].[runtime_leases]
                        WHERE [Id] = @leaseKey
                          AND [OwnerId] = @ownerId)
                    THEN 1 ELSE 0 END AS bit);
            """;
        command.Parameters.Add(new SqlParameter("@leaseKey", leaseKey));
        command.Parameters.Add(new SqlParameter("@ownerId", ownerId));
        command.Parameters.Add(new SqlParameter("@acquiredTimeLocal", acquiredTimeLocal));
        command.Parameters.Add(new SqlParameter("@expiresAtLocal", expiresAtLocal));

        if (await command.ExecuteScalarAsync(ct) is bool acquiredResult)
        {
            return acquiredResult;
        }

        return false;
    }

    /// <summary>
    /// 读取当前连接上下文中的租约快照。
    /// </summary>
    /// <param name="connection">已打开的 SQL 连接。</param>
    /// <param name="leaseKey">租约键。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>租约存在时返回快照；否则返回 <see langword="null"/>。</returns>
    private static async Task<RuntimeLeaseSnapshot?> LoadLeaseSnapshotAsync(
        SqlConnection connection,
        string leaseKey,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT [Id], [OwnerId], [AcquiredTimeLocal], [ExpiresAtLocal]
            FROM [dbo].[runtime_leases]
            WHERE [Id] = @leaseKey;
            """;
        command.Parameters.Add(new SqlParameter("@leaseKey", leaseKey));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new RuntimeLeaseSnapshot
        {
            LeaseKey = reader.GetString(0),
            OwnerId = reader.GetString(1),
            AcquiredTimeLocal = reader.GetDateTime(2),
            ExpiresAtLocal = reader.GetDateTime(3)
        };
    }

    /// <summary>
    /// 尝试接管同机已失活进程持有的租约。
    /// </summary>
    /// <param name="connection">已打开的 SQL 连接。</param>
    /// <param name="activeLease">当前数据库中的租约快照。</param>
    /// <param name="newOwnerId">新持有者标识。</param>
    /// <param name="acquiredTimeLocal">新获取时间。</param>
    /// <param name="expiresAtLocal">新过期时间。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>接管成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    private static async Task<bool> TryTakeOverAbandonedLeaseAsync(
        SqlConnection connection,
        RuntimeLeaseSnapshot activeLease,
        string newOwnerId,
        DateTime acquiredTimeLocal,
        DateTime expiresAtLocal,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE [dbo].[runtime_leases]
            SET [OwnerId] = @newOwnerId,
                [AcquiredTimeLocal] = @acquiredTimeLocal,
                [ExpiresAtLocal] = @expiresAtLocal
            WHERE [Id] = @leaseKey
              AND [OwnerId] = @previousOwnerId;

            SELECT
                CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS bit);
            """;
        command.Parameters.Add(new SqlParameter("@leaseKey", activeLease.LeaseKey));
        command.Parameters.Add(new SqlParameter("@newOwnerId", newOwnerId));
        command.Parameters.Add(new SqlParameter("@acquiredTimeLocal", acquiredTimeLocal));
        command.Parameters.Add(new SqlParameter("@expiresAtLocal", expiresAtLocal));
        command.Parameters.Add(new SqlParameter("@previousOwnerId", activeLease.OwnerId));

        if (await command.ExecuteScalarAsync(ct) is bool takeoverResult)
        {
            return takeoverResult;
        }

        return false;
    }

    /// <summary>
    /// 判断当前租约是否属于同机已失活进程。
    /// </summary>
    /// <param name="ownerId">待判断的持有者标识。</param>
    /// <returns>属于同机已失活进程返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    private static bool ShouldRecoverAbandonedLease(string ownerId)
    {
        if (!RuntimeLeaseOwnerId.TryParse(ownerId, out var descriptor))
        {
            return false;
        }

        if (!string.Equals(descriptor.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            || descriptor.ProcessId == Environment.ProcessId)
        {
            return false;
        }

        try
        {
            using (var process = Process.GetProcessById(descriptor.ProcessId))
            {
                return process.HasExited;
            }
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }
}
