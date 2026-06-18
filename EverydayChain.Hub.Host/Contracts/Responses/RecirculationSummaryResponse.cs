namespace EverydayChain.Hub.Host.Contracts.Responses;

public sealed class RecirculationSummaryResponse
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public string? SelectedChuteCode { get; set; }

    public string SortOrder { get; set; } = "Most";

    public IReadOnlyList<RecirculationSummaryRowResponse> Rows { get; set; } = [];
}
