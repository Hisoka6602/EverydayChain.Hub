namespace EverydayChain.Hub.Host.Contracts.Responses;

public sealed class RecirculationSummaryRowResponse
{
    public string Chute { get; set; } = string.Empty;

    public string WaveNo { get; set; } = string.Empty;

    public int Reflow { get; set; }
}
