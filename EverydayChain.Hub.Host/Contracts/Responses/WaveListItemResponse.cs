namespace EverydayChain.Hub.Host.Contracts.Responses;

public sealed class WaveListItemResponse
{
    public string WaveId { get; set; } = string.Empty;

    public string? Remark { get; set; }

    public int PackageTotal { get; set; }

    public int SplitTotal { get; set; }

    public int FullTotal { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;
}
