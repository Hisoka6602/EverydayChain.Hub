using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Architecture;

/// <summary>
/// 业务任务索引覆盖门禁测试。
/// </summary>
public class BusinessTaskIndexCoverageTests
{
    /// <summary>
    /// business_tasks 必须保留关键查询与幂等索引。
    /// </summary>
    [Fact]
    public void BusinessTaskModel_ShouldContainRequiredIndexes()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(BusinessTaskEntity));
        Assert.NotNull(entityType);

        var indexes = entityType!.GetIndexes()
            .Select(index => new
            {
                Columns = index.Properties.Select(property => property.Name).ToArray(),
                index.IsUnique
            })
            .ToList();

        Assert.Contains(indexes, index => index.IsUnique && index.Columns.SequenceEqual(["SourceTableCode", "BusinessKey"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["NormalizedBarcode"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["NormalizedWaveCode"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["ResolvedDockCode"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["Status"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["FeedbackStatus"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["CreatedTimeLocal"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["UpdatedTimeLocal"]));
    }

    /// <summary>
    /// 创建测试用数据库上下文。
    /// </summary>
    /// <returns>数据库上下文实例。</returns>
    private static HubDbContext CreateDbContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("HUB_TEST_SQLSERVER_CONNECTION_STRING")
            ?? "Server=127.0.0.1;Database=Placeholder;Trusted_Connection=True;TrustServerCertificate=True;";
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
