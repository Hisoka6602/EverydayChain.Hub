namespace EverydayChain.Hub.Application.Models;

public sealed class WaveListQueryResult
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public IReadOnlyList<WaveListItem> Items { get; set; } = [];
}
