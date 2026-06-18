namespace EverydayChain.Hub.Application.Models;

public sealed class WaveListItem
{
    public string WaveCode { get; set; } = string.Empty;

    public string? WaveRemark { get; set; }

    public int PackageTotal { get; set; }

    public int SplitTotal { get; set; }

    public int FullCaseTotal { get; set; }

    public DateTime CreatedTimeLocal { get; set; }

    public string Status { get; set; } = string.Empty;
}
