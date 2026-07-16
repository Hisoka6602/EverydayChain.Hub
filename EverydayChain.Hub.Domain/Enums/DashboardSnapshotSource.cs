using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 表示看板快照数据源类型。
/// </summary>
public enum DashboardSnapshotSource
{
    /// <summary>
    /// 业务任务聚合快照。
    /// </summary>
    [Description("业务任务聚合快照")]
    BusinessTask = 1,

    /// <summary>
    /// 扫描日志聚合快照。
    /// </summary>
    [Description("扫描日志聚合快照")]
    ScanLog = 2,

    /// <summary>
    /// 当前波次分钟快照。
    /// </summary>
    [Description("当前波次分钟快照")]
    CurrentWave = 3,
}
