using EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义 DashboardTaskSnapshotEntityTypeConfiguration 类型。
/// </summary>
public sealed class DashboardTaskSnapshotEntityTypeConfiguration : IEntityTypeConfiguration<DashboardTaskSnapshotEntity>
{
    /// <summary>
    /// 存储 _tableName 字段。
    /// </summary>
    private readonly string _tableName;
    /// <summary>
    /// 存储 _schema 字段。
    /// </summary>
    private readonly string _schema;

    public DashboardTaskSnapshotEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<DashboardTaskSnapshotEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.WaveCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.WaveRemark).HasMaxLength(128);
        builder.Property(x => x.ResolvedDockCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.WorkingArea).HasMaxLength(32);
        builder.Property(x => x.BucketStartLocal).IsRequired();
        builder.Property(x => x.SourceType).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.TotalCount).IsRequired();
        builder.Property(x => x.ScannedCount).IsRequired();
        builder.Property(x => x.RecirculatedCount).IsRequired();
        builder.Property(x => x.ExceptionCount).IsRequired();
        builder.Property(x => x.RequiredFeedbackCount).IsRequired();
        builder.Property(x => x.CompletedFeedbackCount).IsRequired();
        builder.Property(x => x.TotalVolumeMm3).HasColumnType("decimal(18,3)");
        builder.Property(x => x.TotalWeightGram).HasColumnType("decimal(18,3)");
        builder.Property(x => x.EarliestCreatedTimeLocal).IsRequired();
        builder.Property(x => x.LatestUpdatedTimeLocal).IsRequired();
        builder.HasIndex(x => x.BucketStartLocal);
        builder.HasIndex(x => new { x.BucketStartLocal, x.WaveCode });
        builder.HasIndex(x => new { x.BucketStartLocal, x.ResolvedDockCode });
        builder.HasIndex(x => new { x.BucketStartLocal, x.WaveCode, x.ResolvedDockCode });
        builder.HasIndex(x => new { x.BucketStartLocal, x.WaveCode, x.SourceType, x.Status });
        builder.HasIndex(x => new { x.BucketStartLocal, x.WaveCode, x.WorkingArea, x.SourceType, x.Status });
    }
}

