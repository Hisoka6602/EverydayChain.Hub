namespace EverydayChain.Hub.Host.Contracts.Responses;

public sealed class CurrentWaveResponse
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public string? WaveCode { get; set; }

    public string? WaveRemark { get; set; }

    public string? Barcode { get; set; }

    public DateTime? ScanTimeLocal { get; set; }
}
