using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// <see cref="SortingTaskTraceEntity"/> 的 EF Core Fluent API 类型配置，
/// 支持运行时动态切换表名以实现分表路由。
/// </summary>
public class SortingTaskTraceEntityTypeConfiguration : IEntityTypeConfiguration<SortingTaskTraceEntity>
{
    /// <summary>目标表名（含分表后缀）。</summary>
    private readonly string _tableName;

    /// <summary>目标 Schema 名称。</summary>
    private readonly string _schema;

    /// <summary>
    /// 初始化配置实例。
    /// </summary>
    /// <param name="tableName">含后缀的完整表名，例如 <c>sorting_task_trace_202603</c>。</param>
    /// <param name="schema">数据库 Schema，例如 <c>dbo</c>。</param>
    public SortingTaskTraceEntityTypeConfiguration(string tableName, string schema)
    {
        _tableName = tableName;
        _schema = schema;
    }

    /// <inheritdoc/>
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
