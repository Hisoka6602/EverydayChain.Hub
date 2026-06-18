namespace EverydayChain.Hub.Application.Models;

public sealed class ExportCatalogQueryResult
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public DateTime GeneratedTimeLocal { get; set; }

    public IReadOnlyList<ExportCatalogItem> Items { get; set; } = [];
}
