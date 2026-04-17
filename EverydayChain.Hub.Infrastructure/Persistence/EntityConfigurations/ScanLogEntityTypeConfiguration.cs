using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// <see cref="ScanLogEntity"/> EF Core 实体配置，映射到固定表 <c>scan_logs</c>。
/// </summary>
public class ScanLogEntityTypeConfiguration : IEntityTypeConfiguration<ScanLogEntity>
{
    /// <summary>物理表名（已含 Schema 信息由外部传入）。</summary>
    private readonly string _tableName;

    /// <summary>目标 Schema。</summary>
    private readonly string _schema;

    /// <summary>
    /// 初始化 ScanLogEntityTypeConfiguration。
    /// </summary>
    /// <param name="tableName">物理表名。</param>
    /// <param name="schema">目标 Schema。</param>
    public ScanLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ScanLogEntity> builder)
    {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Barcode).IsRequired().HasMaxLength(128);
        builder.Property(x => x.TaskCode).HasMaxLength(64);
        builder.Property(x => x.DeviceCode).HasMaxLength(64);
        builder.Property(x => x.FailureReason).HasMaxLength(256);
        builder.Property(x => x.TraceId).HasMaxLength(64);
        builder.HasIndex(x => x.BusinessTaskId);
        builder.HasIndex(x => x.Barcode);
        builder.HasIndex(x => x.TaskCode);
        builder.HasIndex(x => x.ScanTimeLocal);
        builder.HasIndex(x => new { x.Barcode, x.ScanTimeLocal });
        builder.HasIndex(x => new { x.TaskCode, x.ScanTimeLocal });
    }
}
