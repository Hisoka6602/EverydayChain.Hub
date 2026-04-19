namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务模拟补数命令。
/// </summary>
public sealed class BusinessTaskSeedCommand
{
    /// <summary>
    /// 目标物理表名。
    /// </summary>
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 待补数条码集合。
    /// </summary>
    public IReadOnlyList<string> Barcodes { get; set; } = [];
}
