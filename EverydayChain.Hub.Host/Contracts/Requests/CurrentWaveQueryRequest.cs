namespace EverydayChain.Hub.Host.Contracts.Requests;

public sealed class CurrentWaveQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }
}
