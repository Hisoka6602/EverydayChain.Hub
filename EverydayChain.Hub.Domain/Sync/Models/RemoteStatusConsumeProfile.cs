namespace EverydayChain.Hub.Domain.Sync.Models;

/// <summary>
/// 状态驱动消费配置，描述 StatusDriven 模式下读取、追加与回写的参数。
/// 仅当 SyncMode = StatusDriven 时生效。
/// </summary>
public class RemoteStatusConsumeProfile
{
    /// <summary>
    /// 状态列名（源端真实列名，仅允许字母、数字、下划线）。
    /// 用于筛选待处理行和执行回写更新。默认值：TASKPROCESS。
    /// </summary>
    public string StatusColumnName { get; set; } = "TASKPROCESS";

    /// <summary>
    /// 待处理状态值（字符串或 null）。
    /// 非 null 时生成 StatusColumnName = PendingStatusValue 查询条件；
    /// null 时生成 StatusColumnName IS NULL 查询条件。
    /// 默认值：N。
    /// </summary>
    public string? PendingStatusValue { get; set; } = "N";

    /// <summary>
    /// 完成状态值（仅在 ShouldWriteBackRemoteStatus=true 时使用）。
    /// 回写时将远端行的 StatusColumnName 更新为此值。默认值：Y。
    /// </summary>
    public string CompletedStatusValue { get; set; } = "Y";

    /// <summary>
    /// 是否回写远端状态（可选，取值 true/false）。
    /// true：本地追加成功后，按 ROWID 将远端行状态更新为 CompletedStatusValue；
    /// false：仅本地追加，不执行任何远端状态更新。
    /// 默认值：true。
    /// </summary>
    public bool ShouldWriteBackRemoteStatus { get; set; } = true;

    /// <summary>
    /// 单页读取批次大小（建议范围：1~100000）。
    /// 控制每次从 Oracle 读取的最大行数，读完一页后立即追加并回写，再读下一页。
    /// 默认值：5000。
    /// </summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>
    /// 回写完成时间审计列名（可选，取值范围：源端真实列名，仅允许字母、数字、下划线）。
    /// 配置后回写时会将该列更新为当前本地时间；未配置时不更新该审计列。
    /// </summary>
    public string? WriteBackCompletedTimeColumnName { get; set; }

    /// <summary>
    /// 回写批次号审计列名（可选，取值范围：源端真实列名，仅允许字母、数字、下划线）。
    /// 配置后回写时会将该列更新为当前同步批次号；未配置时不更新该审计列。
    /// </summary>
    public string? WriteBackBatchIdColumnName { get; set; }
}
