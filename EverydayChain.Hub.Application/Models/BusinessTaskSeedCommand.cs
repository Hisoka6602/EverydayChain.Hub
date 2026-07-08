namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskSeedCommand 类型。
/// </summary>
public sealed class BusinessTaskSeedCommand
{
    /// <summary>
    /// 获取或设置 TargetTableName。
    /// </summary>
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Barcodes。
    /// </summary>
    public IReadOnlyList<string> Barcodes { get; set; } = [];
}

