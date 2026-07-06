using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class RuntimeLeaseRepository(IDbContextFactory<HubDbContext> dbContextFactory) : IRuntimeLeaseRepository
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public async Task<bool> TryAcquireAsync(
        string leaseKey,
        string ownerId,
        DateTime acquiredTimeLocal,
        DateTime expiresAtLocal,
        CancellationToken ct)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        var connection = (SqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

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

    public async Task ReleaseAsync(string leaseKey, string ownerId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        await dbContext.RuntimeLeases
            .Where(lease => lease.Id == leaseKey && lease.OwnerId == ownerId)
            .ExecuteDeleteAsync(ct);
    }
}

