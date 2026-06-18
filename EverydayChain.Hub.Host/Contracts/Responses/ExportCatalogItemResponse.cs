namespace EverydayChain.Hub.Host.Contracts.Responses;

public sealed class ExportCatalogItemResponse
{
    public string Key { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
