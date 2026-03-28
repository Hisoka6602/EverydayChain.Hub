using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.SortingTaskTraceAggregate;

public class SortingTaskTraceEntity {
    [Key]
    public long Id { get; set; }

    [MaxLength(32)]
    public string BusinessNo { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Channel { get; set; } = string.Empty;

    [MaxLength(64)]
    public string StationCode { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    [MaxLength(512)]
    public string? Payload { get; set; }
}
