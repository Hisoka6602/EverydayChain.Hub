using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 同步窗口计算器接口。
/// </summary>
public interface ISyncWindowCalculator
{
    /// <summary>
    /// 计算同步窗口。
    /// </summary>
    /// <param name="definition">表定义。</param>
    /// <param name="checkpoint">检查点。</param>
    /// <param name="nowLocal">当前本地时间。</param>
    /// <returns>同步窗口。</returns>
    SyncWindow CalculateWindow(SyncTableDefinition definition, SyncCheckpoint checkpoint, DateTime nowLocal);
}
