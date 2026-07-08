using EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 配置当前波次分钟快照实体的数据库映射。
/// </summary>
public sealed class DashboardCurrentWaveSnapshotEntityTypeConfiguration : IEntityTypeConfiguration<DashboardCurrentWaveSnapshotEntity>
{
    /// <summary>
    /// 存储表名。
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// 存储架构名。
    /// </summary>
    private readonly string _schema;

    /// <summary>
    /// 初始化当前波次分钟快照实体映射配置。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="schema">架构名。</param>
    public DashboardCurrentWaveSnapshotEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <summary>
    /// 配置当前波次分钟快照实体映射。
    /// </summary>
    /// <param name="builder">实体构建器。</param>
    public void Configure(EntityTypeBuilder<DashboardCurrentWaveSnapshotEntity> builder)
    {
        // 步骤：配置表、主键、字段长度以及分钟级查询所需索引。
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BucketStartLocal).IsRequired();
        builder.Property(x => x.ScannedAtLocal).IsRequired();
        builder.Property(x => x.WaveCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.WaveRemark).HasMaxLength(128);
        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.BucketStartLocal).IsUnique();
        builder.HasIndex(x => new { x.BucketStartLocal, x.ScannedAtLocal });
    }
}
