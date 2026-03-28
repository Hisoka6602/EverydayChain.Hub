using System;
using EverydayChain.Hub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations;

[DbContext(typeof(HubDbContext))]
partial class HubDbContextModelSnapshot : ModelSnapshot {
    protected override void BuildModel(ModelBuilder modelBuilder) {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.14")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate.SortingTaskTraceEntity", b => {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("bigint")
                .HasAnnotation("SqlServer:Identity", "1, 1");

            b.Property<string>("BusinessNo")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("nvarchar(32)");

            b.Property<string>("Channel")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("nvarchar(32)");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("datetimeoffset");

            b.Property<string>("Payload")
                .HasMaxLength(512)
                .HasColumnType("nvarchar(512)");

            b.Property<string>("StationCode")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("nvarchar(64)");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("nvarchar(32)");

            b.HasKey("Id");
            b.HasIndex("BusinessNo");
            b.HasIndex("CreatedAt");
            b.ToTable("sorting_task_trace", "dbo");
        });
#pragma warning restore 612, 618
    }
}
