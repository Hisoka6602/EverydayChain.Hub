namespace EverydayChain.Hub.Application.Models;

public sealed class WaveListQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }
}
