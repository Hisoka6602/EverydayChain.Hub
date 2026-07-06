using EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DashboardScanSnapshotEntityTypeConfiguration : IEntityTypeConfiguration<DashboardScanSnapshotEntity>
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly string _tableName;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly string _schema;

    public DashboardScanSnapshotEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<DashboardScanSnapshotEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BucketStartLocal).IsRequired();
        builder.Property(x => x.TotalScanCount).IsRequired();
        builder.Property(x => x.MatchedScanCount).IsRequired();
        builder.HasIndex(x => x.BucketStartLocal).IsUnique();
    }
}

