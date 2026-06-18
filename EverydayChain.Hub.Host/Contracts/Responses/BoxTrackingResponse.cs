namespace EverydayChain.Hub.Host.Contracts.Responses;

public sealed class BoxTrackingResponse
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public IReadOnlyList<BoxTrackingItemResponse> Items { get; set; } = [];
}
