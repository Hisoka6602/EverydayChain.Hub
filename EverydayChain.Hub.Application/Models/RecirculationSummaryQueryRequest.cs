namespace EverydayChain.Hub.Application.Models;

public sealed class RecirculationSummaryQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public string? ChuteCode { get; set; }

    public string SortOrder { get; set; } = "Most";
}
