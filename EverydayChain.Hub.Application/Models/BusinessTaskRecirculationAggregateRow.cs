namespace EverydayChain.Hub.Application.Models;

public sealed class BusinessTaskRecirculationAggregateRow
{
    public string ChuteCode { get; set; } = string.Empty;

    public string WaveCode { get; set; } = string.Empty;

    public int RecirculatedCount { get; set; }
}
