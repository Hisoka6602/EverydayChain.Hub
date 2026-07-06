using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Abstractions.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISyncWindowCalculator
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    SyncWindow CalculateWindow(SyncTableDefinition definition, SyncCheckpoint checkpoint, DateTime nowLocal);
}

