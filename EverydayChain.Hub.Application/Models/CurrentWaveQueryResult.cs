namespace EverydayChain.Hub.Application.Models;

public sealed class CurrentWaveQueryResult
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public string? WaveCode { get; set; }

    public string? WaveRemark { get; set; }

    public string? Barcode { get; set; }

    public DateTime? ScanTimeLocal { get; set; }
}
