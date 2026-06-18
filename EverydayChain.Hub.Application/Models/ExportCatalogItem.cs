namespace EverydayChain.Hub.Application.Models;

public sealed class ExportCatalogItem
{
    public string Key { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public DateTime UpdatedTimeLocal { get; set; }
}
