using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义 BusinessTaskEntityTypeConfiguration 类型。
/// </summary>
public class BusinessTaskEntityTypeConfiguration : IEntityTypeConfiguration<BusinessTaskEntity>
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
    /// 初始化业务任务实体映射配置。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="schema">架构名。</param>
    public BusinessTaskEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <summary>
    /// 配置业务任务实体映射。
    /// </summary>
    /// <param name="builder">实体构建器。</param>
    public void Configure(EntityTypeBuilder<BusinessTaskEntity> builder)
    {
        // 步骤：配置字段约束，并为波次、码头、chute 和时间回退查询建立热点索引。
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TaskCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceTableCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceType).IsRequired();
        builder.Property(x => x.BusinessKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Barcode).HasMaxLength(128);
        builder.Property(x => x.NormalizedBarcode).HasMaxLength(128);
        builder.Property(x => x.TargetChuteCode).HasMaxLength(64);
        builder.Property(x => x.ActualChuteCode).HasMaxLength(64);
        builder.Property(x => x.ResolvedDockCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DeviceCode).HasMaxLength(64);
        builder.Property(x => x.TraceId).HasMaxLength(64);
        builder.Property(x => x.FailureReason).HasMaxLength(256);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.FeedbackStatus).IsRequired();
        builder.Property(x => x.LengthMm).HasColumnType("decimal(18,3)");
        builder.Property(x => x.WidthMm).HasColumnType("decimal(18,3)");
        builder.Property(x => x.HeightMm).HasColumnType("decimal(18,3)");
        builder.Property(x => x.VolumeMm3).HasColumnType("decimal(18,3)");
        builder.Property(x => x.WeightGram).HasColumnType("decimal(18,3)");
        builder.Property(x => x.ScanCount).IsRequired();
        builder.Property(x => x.CreatedTimeLocal).IsRequired();
        builder.Property(x => x.UpdatedTimeLocal).IsRequired();
        builder.Property(x => x.WaveRemark).HasMaxLength(128);
        builder.Property(x => x.WorkingArea).HasMaxLength(32);
        builder.Property(x => x.OrderId).HasMaxLength(64);
        builder.Property(x => x.StoreId).HasMaxLength(64);
        builder.Property(x => x.StoreName).HasMaxLength(300);
        builder.Property(x => x.ProductCode).HasMaxLength(64);
        builder.Property(x => x.PickLocation).HasMaxLength(64);
        builder.Property(x => x.NormalizedWaveCode).HasMaxLength(64);
        builder.Property(x => x.IsException).IsRequired();
        builder.Property(x => x.IsFeedbackReported).IsRequired();
        builder.HasIndex(x => x.TaskCode).IsUnique();
        builder.HasIndex(x => new { x.SourceTableCode, x.BusinessKey }).IsUnique();
        builder.HasIndex(x => x.Barcode);
        builder.HasIndex(x => x.NormalizedBarcode);
        builder.HasIndex(x => new { x.Barcode, x.CreatedTimeLocal });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SourceType);
        builder.HasIndex(x => x.IsException);
        builder.HasIndex(x => x.WorkingArea);
        builder.HasIndex(x => x.FeedbackStatus);
        builder.HasIndex(x => x.IsFeedbackReported);
        builder.HasIndex(x => x.FeedbackTimeLocal);
        builder.HasIndex(x => x.CreatedTimeLocal);
        builder.HasIndex(x => x.UpdatedTimeLocal);
        builder.HasIndex(x => x.ScannedAtLocal);
        builder.HasIndex(x => x.DroppedAtLocal);
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.Id });
        builder.Property(x => x.WaveCode).HasMaxLength(64);
        builder.HasIndex(x => x.NormalizedWaveCode);
        builder.HasIndex(x => x.WaveCode);
        builder.HasIndex(x => x.TargetChuteCode);
        builder.HasIndex(x => x.ActualChuteCode);
        builder.HasIndex(x => x.ResolvedDockCode);
        builder.HasIndex(x => new { x.NormalizedBarcode, x.CreatedTimeLocal });
        builder.HasIndex(x => new { x.WaveCode, x.CreatedTimeLocal });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.NormalizedWaveCode });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.ResolvedDockCode });
        builder.HasIndex(x => new { x.NormalizedWaveCode, x.CreatedTimeLocal });
        builder.HasIndex(x => new { x.NormalizedWaveCode, x.UpdatedTimeLocal });
        builder.HasIndex(x => new { x.NormalizedWaveCode, x.SourceType, x.WorkingArea });
        builder.HasIndex(x => new { x.TargetChuteCode, x.CreatedTimeLocal });
        builder.HasIndex(x => new { x.ActualChuteCode, x.CreatedTimeLocal });
        builder.HasIndex(x => new { x.ResolvedDockCode, x.CreatedTimeLocal });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.ScannedAtLocal, x.Id });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.Status, x.SourceType });
        builder.HasIndex(x => new { x.FeedbackStatus, x.CreatedTimeLocal });
        builder.HasIndex(x => new { x.FeedbackStatus, x.IsFeedbackReported, x.FeedbackTimeLocal });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.SourceType, x.Status, x.IsException, x.ResolvedDockCode });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.NormalizedWaveCode, x.ResolvedDockCode });
    }
}

