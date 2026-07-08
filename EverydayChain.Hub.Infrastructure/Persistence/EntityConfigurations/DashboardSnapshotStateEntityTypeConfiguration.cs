using EverydayChain.Hub.Domain.Aggregates.DashboardSnapshotAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义 DashboardSnapshotStateEntityTypeConfiguration 类型。
/// </summary>
public sealed class DashboardSnapshotStateEntityTypeConfiguration : IEntityTypeConfiguration<DashboardSnapshotStateEntity>
{
    /// <summary>
    /// 存储 _tableName 字段。
    /// </summary>
    private readonly string _tableName;
    /// <summary>
    /// 存储 _schema 字段。
    /// </summary>
    private readonly string _schema;

    public DashboardSnapshotStateEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<DashboardSnapshotStateEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CoverageStartLocal);
        builder.Property(x => x.CoverageEndLocal);
        builder.Property(x => x.LastRefreshTimeLocal);
    }
}

