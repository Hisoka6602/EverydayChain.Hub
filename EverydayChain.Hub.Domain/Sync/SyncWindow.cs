namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 定义当前类型。
/// </summary>
public readonly record struct SyncWindow(DateTime WindowStartLocal, DateTime WindowEndLocal);


