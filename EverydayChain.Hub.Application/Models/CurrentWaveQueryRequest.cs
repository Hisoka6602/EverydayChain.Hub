namespace EverydayChain.Hub.Application.Models;

public sealed class CurrentWaveQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }
}
