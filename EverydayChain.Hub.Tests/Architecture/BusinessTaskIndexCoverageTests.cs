using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EverydayChain.Hub.Tests.Architecture;

/// <summary>
/// 定义当前类型。
/// </summary>
public class BusinessTaskIndexCoverageTests
{
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
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["NormalizedBarcode", "CreatedTimeLocal"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["TargetChuteCode", "CreatedTimeLocal"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["ActualChuteCode", "CreatedTimeLocal"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["CreatedTimeLocal", "ScannedAtLocal", "Id"]));
        Assert.Contains(indexes, index => !index.IsUnique && index.Columns.SequenceEqual(["CreatedTimeLocal", "SourceType", "Status", "IsException", "ResolvedDockCode"]));
    }

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

