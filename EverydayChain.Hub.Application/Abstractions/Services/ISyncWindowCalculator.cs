using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义 ISyncWindowCalculator 类型。
/// </summary>
public interface ISyncWindowCalculator
{
    /// <summary>
    /// 执行 CalculateWindow 方法。
    /// </summary>
    SyncWindow CalculateWindow(SyncTableDefinition definition, SyncCheckpoint checkpoint, DateTime nowLocal);
}

