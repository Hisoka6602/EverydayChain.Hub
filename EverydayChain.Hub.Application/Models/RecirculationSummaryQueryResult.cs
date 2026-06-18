namespace EverydayChain.Hub.Application.Models;

public sealed class RecirculationSummaryQueryResult
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public string? SelectedChuteCode { get; set; }

    public string SortOrder { get; set; } = "Most";

    public IReadOnlyList<RecirculationSummaryRow> Rows { get; set; } = [];
}
