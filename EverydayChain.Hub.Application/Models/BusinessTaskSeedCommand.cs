namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskSeedCommand
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<string> Barcodes { get; set; } = [];
}

