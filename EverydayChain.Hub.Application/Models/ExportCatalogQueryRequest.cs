namespace EverydayChain.Hub.Application.Models;

public sealed class ExportCatalogQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }
}
