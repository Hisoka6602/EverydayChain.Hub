using EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EverydayChain.Hub.Infrastructure.Persistence.EntityConfigurations;

public class SortingTaskTraceEntityTypeConfiguration : IEntityTypeConfiguration<SortingTaskTraceEntity> {
    private readonly string _tableName;
    private readonly string _schema;

    public SortingTaskTraceEntityTypeConfiguration(string tableName, string schema) {
        _tableName = tableName;
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<SortingTaskTraceEntity> builder) {
        builder.ToTable(_tableName, _schema);
        builder.HasKey(x => x.Id);
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
