using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义 SortingTaskTraceEntityTypeConfiguration 类型。
/// </summary>
public class SortingTaskTraceEntityTypeConfiguration : IEntityTypeConfiguration<SortingTaskTraceEntity>
{
    /// <summary>
    /// 存储 _tableName 字段。
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// 存储 _schema 字段。
    /// </summary>
    private readonly string _schema;

    public SortingTaskTraceEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<SortingTaskTraceEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BusinessNo).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
        builder.Property(x => x.StationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Payload).HasMaxLength(512);
        builder.HasIndex(x => x.BusinessNo);
        builder.HasIndex(x => x.CreatedAt);
    }
}

