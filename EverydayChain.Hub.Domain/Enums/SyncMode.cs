using System.ComponentModel;

namespace EverydayChain.Hub.Domain.Enums;

/// <summary>
/// 同步执行模式，决定同步链路的读取策略与写入策略。
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// 键控合并模式（默认）：按游标窗口分页增量读取，执行幂等 Upsert 合并，并支持缺失删除识别。
    /// 适用于有稳定游标列（如时间戳/自增 ID）的源表。
    /// </summary>
    [Description("键控合并模式")]
    KeyedMerge = 1,

    /// <summary>
    /// 状态驱动消费模式：按状态列值读取待处理行，仅追加写入本地目标表，不执行 Merge/删除链路；
    /// 可选回写远端行状态（按 ROWID）。
    /// 适用于源端有明确"待处理/已完成"状态字段的队列式数据表。
    /// </summary>
    [Description("状态驱动消费模式")]
    StatusDriven = 2,
}
