namespace EverydayChain.Hub.Application.Models;

public sealed class BoxTrackingQueryResult
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public IReadOnlyList<BoxTrackingItem> Items { get; set; } = [];
}
