namespace EverydayChain.Hub.Host.Contracts.Responses;

public sealed class ExportCatalogResponse
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }

    public DateTime GeneratedTimeLocal { get; set; }

    public IReadOnlyList<ExportCatalogItemResponse> Items { get; set; } = [];
}
