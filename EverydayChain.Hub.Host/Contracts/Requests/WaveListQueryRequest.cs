namespace EverydayChain.Hub.Host.Contracts.Requests;

public sealed class WaveListQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }
}
