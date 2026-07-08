using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义 ScanLogEntityTypeConfiguration 类型。
/// </summary>
public class ScanLogEntityTypeConfiguration : IEntityTypeConfiguration<ScanLogEntity>
{
    /// <summary>
    /// 存储 _tableName 字段。
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// 存储 _schema 字段。
    /// </summary>
    private readonly string _schema;

    /// <summary>
    /// 初始化扫描日志实体映射配置。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="schema">架构名。</param>
    public ScanLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <summary>
    /// 配置扫描日志实体映射。
    /// </summary>
    /// <param name="builder">实体构建器。</param>
    public void Configure(EntityTypeBuilder<ScanLogEntity> builder)
    {
        // 步骤：配置字段约束，并为按时间分页、条码筛选、设备筛选建立热点索引。
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
        builder.HasIndex(x => x.DeviceCode);
        builder.HasIndex(x => x.TaskCode);
        builder.HasIndex(x => x.ScanTimeLocal);
        builder.HasIndex(x => x.CreatedTimeLocal);
        builder.HasIndex(x => new { x.Barcode, x.ScanTimeLocal });
        builder.HasIndex(x => new { x.DeviceCode, x.ScanTimeLocal });
        builder.HasIndex(x => new { x.ScanTimeLocal, x.Id });
        builder.HasIndex(x => new { x.TaskCode, x.ScanTimeLocal });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.ScanTimeLocal });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.TaskCode });
    }
}

