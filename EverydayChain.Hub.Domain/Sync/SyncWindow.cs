namespace EverydayChain.Hub.Domain.Sync;

/// <summary>
/// 同步窗口值对象，定义本次同步的起止本地时间。
/// </summary>
/// <param name="WindowStartLocal">窗口起始本地时间。</param>
/// <param name="WindowEndLocal">窗口结束本地时间。</param>
public readonly record struct SyncWindow(DateTime WindowStartLocal, DateTime WindowEndLocal);
