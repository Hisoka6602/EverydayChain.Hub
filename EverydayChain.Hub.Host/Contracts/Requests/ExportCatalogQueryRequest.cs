namespace EverydayChain.Hub.Host.Contracts.Requests;

public sealed class ExportCatalogQueryRequest
{
    public DateTime StartTimeLocal { get; set; }

    public DateTime EndTimeLocal { get; set; }
}
