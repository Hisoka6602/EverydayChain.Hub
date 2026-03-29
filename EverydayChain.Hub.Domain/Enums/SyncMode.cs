using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 同步模式。
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// 首次全量同步。
    /// </summary>
    [Description("首次全量同步")]
    InitialFull = 1,

    /// <summary>
    /// 增量同步。
    /// </summary>
    [Description("增量同步")]
    Incremental = 2,
}
