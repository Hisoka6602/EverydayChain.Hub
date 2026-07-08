using EverydayChain.Hub.Domain.Aggregates.DropLogAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// 定义 DropLogEntityTypeConfiguration 类型。
/// </summary>
public class DropLogEntityTypeConfiguration : IEntityTypeConfiguration<DropLogEntity>
{
    /// <summary>
    /// 存储 _tableName 字段。
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// 存储 _schema 字段。
    /// </summary>
    private readonly string _schema;

    public DropLogEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

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
        builder.HasIndex(x => x.IsSuccess);
        builder.HasIndex(x => x.DropTimeLocal);
        builder.HasIndex(x => x.CreatedTimeLocal);
        builder.HasIndex(x => new { x.TaskCode, x.DropTimeLocal });
        builder.HasIndex(x => new { x.Barcode, x.DropTimeLocal });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.DropTimeLocal });
        builder.HasIndex(x => new { x.CreatedTimeLocal, x.TaskCode });
    }
}

