using EverydayChain.Hub.Domain.Sync;
using EverydayChain.Hub.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncWindowCalculator(ILogger<SyncWindowCalculator> logger) : ISyncWindowCalculator
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int InitialDstInvalidTimeJumpMinutes = 60;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int SecondaryDstInvalidTimeJumpMinutes = 120;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private const int TertiaryDstInvalidTimeJumpMinutes = 180;

    public SyncWindow CalculateWindow(SyncTableDefinition definition, SyncCheckpoint checkpoint, DateTime nowLocal)
    {
        var windowStartLocal = NormalizeLocalWindowBoundary(
            checkpoint.LastSuccessCursorLocal ?? definition.StartTimeLocal,
            definition.TableCode,
            "窗口起点");
        var normalizedNowLocal = EnsureLocalOrUnspecified(nowLocal, definition.TableCode, "当前时间");
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

    private DateTime NormalizeLocalWindowBoundary(DateTime localTime, string tableCode, string scene)
    {
        var normalizedLocalTime = EnsureLocalOrUnspecified(localTime, tableCode, scene);
        if (!TimeZoneInfo.Local.IsInvalidTime(normalizedLocalTime))
        {
            return normalizedLocalTime;
        }

        var adjustedLocalTime = TryResolveInvalidLocalTime(
            normalizedLocalTime,
            normalizedLocalTime.AddMinutes(InitialDstInvalidTimeJumpMinutes),
            InitialDstInvalidTimeJumpMinutes);
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

    private static DateTime TryResolveInvalidLocalTime(DateTime originalLocalTime, DateTime candidateLocalTime, int adjustMinutes)
    {
        if (!TimeZoneInfo.Local.IsInvalidTime(candidateLocalTime))
        {
            return candidateLocalTime;
        }

        return originalLocalTime.AddMinutes(adjustMinutes);
    }

    private DateTime EnsureLocalOrUnspecified(DateTime localTime, string tableCode, string scene)
    {
        if (localTime.Kind is not (DateTimeKind.Local or DateTimeKind.Unspecified))
        {
            logger.LogError(
                "检测到非本地时间语义输入，本地时间窗口计算仅支持 Local/Unspecified。TableCode={TableCode}, Scene={Scene}, InputValue={InputValue}",
                tableCode,
                scene,
                localTime);
            throw new InvalidOperationException(
                $"表 {tableCode} 在场景 {scene} 收到非本地时间语义输入 {localTime:O}，无法按本地时间语义计算窗口。");
        }

        return localTime.Kind == DateTimeKind.Local
            ? localTime
            : DateTime.SpecifyKind(localTime, DateTimeKind.Local);
    }
}

