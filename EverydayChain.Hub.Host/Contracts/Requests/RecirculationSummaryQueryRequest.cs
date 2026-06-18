namespace EverydayChain.Hub.Host.Contracts.Requests;

public sealed class RecirculationSummaryQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public string? ChuteCode { get; set; }

    public string SortOrder { get; set; } = "Most";
}
