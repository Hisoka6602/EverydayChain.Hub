using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义 RuntimeLeaseRepository 类型。
/// </summary>
public sealed class RuntimeLeaseRepository(IDbContextFactory<HubDbContext> dbContextFactory) : IRuntimeLeaseRepository
{
    /// <summary>
    /// 尝试获取指定租约。
    /// </summary>
    public async Task<bool> TryAcquireAsync(
        string leaseKey,
        string ownerId,
        DateTime acquiredTimeLocal,
        DateTime expiresAtLocal,
        CancellationToken ct)
    {
        // 步骤：执行 TryAcquireAsync 方法的核心处理流程。
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var connection = (SqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        // 步骤：先尝试更新已过期租约，再在不存在时插入新租约，最后校验当前实例是否持有该租约。
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

            SELECT CAST(CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM [dbo].[runtime_leases]
                    WHERE [Id] = @leaseKey
                      AND [OwnerId] = @ownerId
                      AND [ExpiresAtLocal] = @expiresAtLocal)
                THEN 1 ELSE 0 END AS bit);
            """;
        command.Parameters.Add(new SqlParameter("@leaseKey", leaseKey));
        command.Parameters.Add(new SqlParameter("@ownerId", ownerId));
        command.Parameters.Add(new SqlParameter("@acquiredTimeLocal", acquiredTimeLocal));
        command.Parameters.Add(new SqlParameter("@expiresAtLocal", expiresAtLocal));
        var acquired = await command.ExecuteScalarAsync(ct);
        return acquired is bool value && value;
    }

    /// <summary>
    /// 释放指定租约。
    /// </summary>
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
}

