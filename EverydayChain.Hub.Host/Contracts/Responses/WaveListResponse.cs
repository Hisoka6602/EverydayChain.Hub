namespace EverydayChain.Hub.Host.Contracts.Responses;

public sealed class WaveListResponse
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public IReadOnlyList<WaveListItemResponse> Items { get; set; } = [];
}
