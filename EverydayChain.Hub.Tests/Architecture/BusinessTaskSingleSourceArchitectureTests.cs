using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace EverydayChain.Hub.Tests.Architecture;

/// <summary>
/// 业务主表单一来源架构防回退测试。
/// </summary>
public class BusinessTaskSingleSourceArchitectureTests
{
    /// <summary>
    /// DbContext 模型中不应再包含本地镜像表映射。
    /// </summary>
    [Fact]
    public void HubDbContextModel_ShouldNotContainLocalIdxMirrorTables()
    {
        using var context = CreateDbContext();
        var tableNames = context.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(tableName => !string.IsNullOrWhiteSpace(tableName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("IDX_PICKTOLIGHT_CARTON1", tableNames);
        Assert.DoesNotContain("IDX_PICKTOWCS2", tableNames);
        Assert.Contains("business_tasks", tableNames);
    }

    /// <summary>
    /// 创建测试用数据库上下文。
    /// </summary>
    /// <returns>数据库上下文实例。</returns>
    private static HubDbContext CreateDbContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("HUB_TEST_SQLSERVER_CONNECTION_STRING")
            ?? "Server=127.0.0.1;Database=Placeholder;User Id=Placeholder;Password=Placeholder;TrustServerCertificate=True;";
        var dbContextOptions = new DbContextOptionsBuilder<HubDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var shardingOptions = Options.Create(new ShardingOptions
        {
            Schema = "dbo"
        });
        return new HubDbContext(dbContextOptions, shardingOptions);
    }
}
