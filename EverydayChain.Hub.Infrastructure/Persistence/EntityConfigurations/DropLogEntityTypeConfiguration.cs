using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// <see cref="DropLogEntity"/> EF Core 实体配置，映射到固定表 <c>drop_logs</c>。
/// </summary>
public class DropLogEntityTypeConfiguration : IEntityTypeConfiguration<DropLogEntity>
{
    /// <summary>物理表名（已含 Schema 信息由外部传入）。</summary>
    private readonly string _tableName;

    /// <summary>目标 Schema。</summary>
    private readonly string _schema;

    /// <summary>
    /// 初始化 DropLogEntityTypeConfiguration。
    /// </summary>
    /// <param name="tableName">物理表名。</param>
    /// <param name="schema">目标 Schema。</param>
    public DropLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DropLogEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TaskCode).HasMaxLength(64);
        builder.Property(x => x.Barcode).HasMaxLength(128);
        builder.Property(x => x.ActualChuteCode).HasMaxLength(64);
        builder.Property(x => x.FailureReason).HasMaxLength(256);
        builder.HasIndex(x => x.BusinessTaskId);
        builder.HasIndex(x => x.TaskCode);
        builder.HasIndex(x => x.Barcode);
        builder.HasIndex(x => x.ActualChuteCode);
        builder.HasIndex(x => x.DropTimeLocal);
        builder.HasIndex(x => new { x.TaskCode, x.DropTimeLocal });
        builder.HasIndex(x => new { x.Barcode, x.DropTimeLocal });
    }
}
