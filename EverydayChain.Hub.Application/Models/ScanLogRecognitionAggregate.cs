namespace EverydayChain.Hub.Application.Models;

public sealed class ScanLogRecognitionAggregate
{
    public int TotalScanCount { get; set; }

    public int MatchedScanCount { get; set; }
}
