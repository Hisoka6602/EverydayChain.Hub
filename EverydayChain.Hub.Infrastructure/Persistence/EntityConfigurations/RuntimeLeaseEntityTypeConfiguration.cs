using EverydayChain.Hub.Domain.Aggregates.RuntimeLeaseAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class RuntimeLeaseEntityTypeConfiguration : IEntityTypeConfiguration<RuntimeLeaseEntity>
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly string _tableName;
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly string _schema;

    public RuntimeLeaseEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<RuntimeLeaseEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.OwnerId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.AcquiredTimeLocal).IsRequired();
        builder.Property(x => x.ExpiresAtLocal).IsRequired();
        builder.HasIndex(x => x.ExpiresAtLocal);
    }
}

