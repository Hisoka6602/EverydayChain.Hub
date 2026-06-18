namespace EverydayChain.Hub.Host.Contracts.Requests;

public sealed class BoxTrackingQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public string? BoxId { get; set; }

    public string? Scanner { get; set; }

    public string? ChuteCode { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}
