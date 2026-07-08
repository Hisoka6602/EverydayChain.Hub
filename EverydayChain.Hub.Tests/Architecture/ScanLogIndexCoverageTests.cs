using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Architecture;

/// <summary>
/// 验证扫描日志模型包含热点查询所需索引。
/// </summary>
public class ScanLogIndexCoverageTests
{
    /// <summary>
    /// 验证扫描日志模型包含高并发分页与筛选所需索引。
    /// </summary>
    [Fact]
    public void ScanLogModel_ShouldContainRequiredIndexes()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(ScanLogEntity));
        Assert.NotNull(entityType);

        var indexes = entityType!.GetIndexes()
            .Select(index => new
            {
                Columns = index.Properties.Select(property => property.Name).ToArray(),
                index.IsUnique
            })
            .ToList();

        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["DeviceCode"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["Barcode", "ScanTimeLocal"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["DeviceCode", "ScanTimeLocal"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["ScanTimeLocal", "Id"]));
    }

    /// <summary>
    /// 创建用于读取模型元数据的数据库上下文。
    /// </summary>
    /// <returns>数据库上下文。</returns>
    private static HubDbContext CreateDbContext()
    {
        /// <summary>
        /// 存储仅用于读取模型元数据的占位连接串。
        /// </summary>
        const string connectionString = "Server=127.0.0.1;Database=Placeholder;User Id=Placeholder;Password=Placeholder;TrustServerCertificate=True;";
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
