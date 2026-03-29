using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 同步窗口计算器实现。
/// </summary>
public class SyncWindowCalculator : ISyncWindowCalculator
{
    /// <inheritdoc/>
    public SyncWindow CalculateWindow(SyncTableDefinition definition, SyncCheckpoint checkpoint, DateTime nowLocal)
    {
        var windowStartLocal = checkpoint.LastSuccessCursorLocal ?? definition.StartTimeLocal;
        var windowEndLocal = nowLocal.AddMinutes(-definition.MaxLagMinutes);
        if (windowEndLocal < windowStartLocal)
        {
            windowEndLocal = windowStartLocal;
        }

        return new SyncWindow(windowStartLocal, windowEndLocal);
    }
}
