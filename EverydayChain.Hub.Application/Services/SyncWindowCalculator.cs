using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 同步窗口计算器实现。
/// </summary>
public class SyncWindowCalculator(ILogger<SyncWindowCalculator> logger) : ISyncWindowCalculator
{
    /// <summary>DST 非法本地时刻初始跳跃分钟数。</summary>
    private const int InitialDstInvalidTimeJumpMinutes = 60;

    /// <summary>DST 非法本地时刻二次跳跃分钟数。</summary>
    private const int SecondaryDstInvalidTimeJumpMinutes = 120;

    /// <summary>DST 非法本地时刻三次跳跃分钟数。</summary>
    private const int TertiaryDstInvalidTimeJumpMinutes = 180;

    /// <inheritdoc/>
    public SyncWindow CalculateWindow(SyncTableDefinition definition, SyncCheckpoint checkpoint, DateTime nowLocal)
    {
        var windowStartLocal = NormalizeLocalWindowBoundary(
            checkpoint.LastSuccessCursorLocal ?? definition.StartTimeLocal,
            definition.TableCode,
            "窗口起点");
        var normalizedNowLocal = nowLocal.Kind == DateTimeKind.Local
            ? nowLocal
            : DateTime.SpecifyKind(nowLocal, DateTimeKind.Local);
        var effectiveNowLocal = ResolveEffectiveNowLocal(normalizedNowLocal, checkpoint.LastSuccessTimeLocal, definition.TableCode);
        var windowEndLocal = NormalizeLocalWindowBoundary(
            effectiveNowLocal.AddMinutes(-definition.MaxLagMinutes),
            definition.TableCode,
            "窗口终点");
        if (windowEndLocal < windowStartLocal)
        {
            windowEndLocal = windowStartLocal;
        }

        return new SyncWindow(windowStartLocal, windowEndLocal);
    }

    /// <summary>
    /// 解析有效的当前本地时间，防止时钟回拨导致窗口倒退。
    /// </summary>
    /// <param name="nowLocal">当前本地时间。</param>
    /// <param name="lastSuccessTimeLocal">上次成功执行时间。</param>
    /// <param name="tableCode">表编码。</param>
    /// <returns>有效当前本地时间。</returns>
    private DateTime ResolveEffectiveNowLocal(DateTime nowLocal, DateTime? lastSuccessTimeLocal, string tableCode)
    {
        if (!lastSuccessTimeLocal.HasValue)
        {
            return nowLocal;
        }

        var normalizedLastSuccessTimeLocal = NormalizeLocalWindowBoundary(lastSuccessTimeLocal.Value, tableCode, "上次成功时间");
        if (nowLocal >= normalizedLastSuccessTimeLocal)
        {
            return nowLocal;
        }

        logger.LogWarning(
            "检测到本地时钟回拨，窗口终点将冻结在上次成功时间以避免重复/漏同步。TableCode={TableCode}, NowLocal={NowLocal}, LastSuccessTimeLocal={LastSuccessTimeLocal}",
            tableCode,
            nowLocal,
            normalizedLastSuccessTimeLocal);
        return normalizedLastSuccessTimeLocal;
    }

    /// <summary>
    /// 规范化窗口边界时间，避免进入 DST 非法本地时刻。
    /// </summary>
    /// <param name="localTime">本地时间。</param>
    /// <param name="tableCode">表编码。</param>
    /// <param name="scene">场景描述。</param>
    /// <returns>规范化后的本地时间。</returns>
    private DateTime NormalizeLocalWindowBoundary(DateTime localTime, string tableCode, string scene)
    {
        var normalizedLocalTime = localTime.Kind == DateTimeKind.Local
            ? localTime
            : DateTime.SpecifyKind(localTime, DateTimeKind.Local);
        if (!TimeZoneInfo.Local.IsInvalidTime(normalizedLocalTime))
        {
            return normalizedLocalTime;
        }

        var adjustedLocalTime = normalizedLocalTime.AddMinutes(InitialDstInvalidTimeJumpMinutes);
        adjustedLocalTime = TryResolveInvalidLocalTime(normalizedLocalTime, adjustedLocalTime);
        adjustedLocalTime = TryResolveInvalidLocalTime(normalizedLocalTime, adjustedLocalTime, SecondaryDstInvalidTimeJumpMinutes);
        adjustedLocalTime = TryResolveInvalidLocalTime(normalizedLocalTime, adjustedLocalTime, TertiaryDstInvalidTimeJumpMinutes);
        if (TimeZoneInfo.Local.IsInvalidTime(adjustedLocalTime))
        {
            throw new InvalidOperationException(
                $"表 {tableCode} 在场景 {scene} 发生 DST 非法时刻修正失败，已尝试顺延 {InitialDstInvalidTimeJumpMinutes}/{SecondaryDstInvalidTimeJumpMinutes}/{TertiaryDstInvalidTimeJumpMinutes} 分钟。");
        }

        logger.LogWarning(
            "检测到 DST 非法本地时间，窗口边界已自动顺延到合法时刻。TableCode={TableCode}, Scene={Scene}, Original={OriginalTime}, Adjusted={AdjustedTime}",
            tableCode,
            scene,
            normalizedLocalTime,
            adjustedLocalTime);
        return adjustedLocalTime;
    }

    /// <summary>
    /// 在指定顺延分钟数下尝试修复 DST 非法时刻。
    /// </summary>
    /// <param name="originalLocalTime">原始本地时间。</param>
    /// <param name="candidateLocalTime">当前候选时间。</param>
    /// <param name="adjustMinutes">顺延分钟数。</param>
    /// <returns>修复后的候选时间。</returns>
    private static DateTime TryResolveInvalidLocalTime(DateTime originalLocalTime, DateTime candidateLocalTime, int adjustMinutes = InitialDstInvalidTimeJumpMinutes)
    {
        if (!TimeZoneInfo.Local.IsInvalidTime(candidateLocalTime))
        {
            return candidateLocalTime;
        }

        return originalLocalTime.AddMinutes(adjustMinutes);
    }
}
