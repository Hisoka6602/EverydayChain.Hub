namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 表示看板快照数据源类型。
/// </summary>
public enum DashboardSnapshotSource
{
    /// <summary>
    /// 业务任务聚合快照。
    /// </summary>
    BusinessTask = 1,

    /// <summary>
    /// 扫描日志聚合快照。
    /// </summary>
    ScanLog = 2,

    /// <summary>
    /// 当前波次分钟快照。
    /// </summary>
    CurrentWave = 3,
}

