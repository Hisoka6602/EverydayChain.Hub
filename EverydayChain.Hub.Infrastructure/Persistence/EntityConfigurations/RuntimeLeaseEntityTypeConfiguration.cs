using EverydayChain.Hub.Domain.Aggregates.RuntimeLeaseAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 配置运行时租约实体的数据库映射。
/// </summary>
public sealed class RuntimeLeaseEntityTypeConfiguration : IEntityTypeConfiguration<RuntimeLeaseEntity>
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
    /// 初始化运行时租约实体映射配置。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="schema">架构名。</param>
    public RuntimeLeaseEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <summary>
    /// 配置运行时租约实体映射。
    /// </summary>
    /// <param name="builder">实体构建器。</param>
    public void Configure(EntityTypeBuilder<RuntimeLeaseEntity> builder)
    {
        // 步骤：配置表、主键、字段长度与按过期时间查询所需索引。
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).HasMaxLength(128);
        builder.Property(x => x.OwnerId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.AcquiredTimeLocal).IsRequired();
        builder.Property(x => x.ExpiresAtLocal).IsRequired();
        builder.HasIndex(x => x.ExpiresAtLocal);
    }
}
