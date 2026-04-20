using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 波次查询服务实现。
/// </summary>
public sealed class WaveQueryService(
    IBusinessTaskRepository businessTaskRepository,
    ILogger<WaveQueryService> logger) : IWaveQueryService
{
    /// <summary>
    /// 分区编码：拆零 1 区。
    /// </summary>
    private const string SplitZone1Code = "SplitZone1";

    /// <summary>
    /// 分区编码：拆零 2 区。
    /// </summary>
    private const string SplitZone2Code = "SplitZone2";

    /// <summary>
    /// 分区编码：拆零 3 区。
    /// </summary>
    private const string SplitZone3Code = "SplitZone3";

    /// <summary>
    /// 分区编码：拆零 4 区。
    /// </summary>
    private const string SplitZone4Code = "SplitZone4";

    /// <summary>
    /// 分区编码：整件。
    /// </summary>
    private const string FullCaseCode = "FullCase";

    /// <summary>
    /// 固定分区输出顺序。
    /// </summary>
    private static readonly IReadOnlyList<string> ZoneOutputOrder =
    [
        SplitZone1Code,
        SplitZone2Code,
        SplitZone3Code,
        SplitZone4Code,
        FullCaseCode
    ];

    /// <summary>
    /// 查询时间区间内的波次选项。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次选项查询结果。</returns>
    public async Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new WaveOptionsQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        var waveRows = await businessTaskRepository.ListWaveOptionsByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
        return new WaveOptionsQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            WaveOptions = waveRows
                .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
                .Select(row => new WaveOptionItem
                {
                    WaveCode = row.WaveCode,
                    WaveRemark = row.WaveRemark
                })
                .ToList()
        };
    }

    /// <summary>
    /// 查询单个波次摘要。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次摘要结果；未命中返回空值。</returns>
    public async Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        var taskStats = await FindWaveTaskStatsAsync(request.StartTimeLocal, request.EndTimeLocal, request.WaveCode, cancellationToken);
        if (taskStats.Count == 0)
        {
            return null;
        }

        var queryPolicy = new BusinessTaskQueryPolicy();
        var totalCount = taskStats.Count;
        var unsortedCount = taskStats.Count(task => !IsSorted(task.Status));
        var sortedCount = totalCount - unsortedCount;
        return new WaveSummaryQueryResult
        {
            WaveCode = request.WaveCode.Trim(),
            WaveRemark = ResolveWaveRemark(taskStats),
            TotalCount = totalCount,
            UnsortedCount = unsortedCount,
            SortedProgressPercent = queryPolicy.CalculatePercent(sortedCount, totalCount),
            RecirculatedCount = taskStats.Count(task => queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode)),
            ExceptionCount = taskStats.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception)
        };
    }

    /// <summary>
    /// 查询单个波次分区明细。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次分区结果；未命中返回空值。</returns>
    public async Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
    {
        var taskStats = await FindWaveTaskStatsAsync(request.StartTimeLocal, request.EndTimeLocal, request.WaveCode, cancellationToken);
        if (taskStats.Count == 0)
        {
            return null;
        }

        var queryPolicy = new BusinessTaskQueryPolicy();
        var zoneMap = BuildZoneAccumulatorMap();
        foreach (var task in taskStats)
        {
            var targetZone = ResolveTargetZone(task);
            if (targetZone is null)
            {
                continue;
            }

            if (!zoneMap.TryGetValue(targetZone, out var zone))
            {
                continue;
            }

            zone.TotalCount++;
            if (!IsSorted(task.Status))
            {
                zone.UnsortedCount++;
            }

            if (queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode))
            {
                zone.RecirculatedCount++;
            }

            if (task.IsException || task.Status == BusinessTaskStatus.Exception)
            {
                zone.ExceptionCount++;
            }
        }

        var zoneSummaries = ZoneOutputOrder
            .Select(zoneCode => zoneMap.TryGetValue(zoneCode, out var zone) ? zone : null)
            .Where(zone => zone is not null)
            .Select(zone => zone!)
            .Select(zone =>
            {
                var sortedCount = zone.TotalCount - zone.UnsortedCount;
                return new WaveZoneSummary
                {
                    ZoneCode = zone.ZoneCode,
                    ZoneName = zone.ZoneName,
                    TotalCount = zone.TotalCount,
                    UnsortedCount = zone.UnsortedCount,
                    SortedProgressPercent = queryPolicy.CalculatePercent(sortedCount, zone.TotalCount),
                    RecirculatedCount = zone.RecirculatedCount,
                    ExceptionCount = zone.ExceptionCount
                };
            })
            .ToList();

        return new WaveZoneQueryResult
        {
            WaveCode = request.WaveCode.Trim(),
            WaveRemark = ResolveWaveRemark(taskStats),
            Zones = zoneSummaries
        };
    }

    /// <summary>
    /// 查询指定波次任务集合。
    /// </summary>
    /// <param name="startTimeLocal">开始时间。</param>
    /// <param name="endTimeLocal">结束时间。</param>
    /// <param name="waveCode">波次号。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务集合。</returns>
    private async Task<IReadOnlyList<BusinessTaskWaveTaskStatsRow>> FindWaveTaskStatsAsync(
        DateTime startTimeLocal,
        DateTime endTimeLocal,
        string waveCode,
        CancellationToken cancellationToken)
    {
        if (endTimeLocal <= startTimeLocal || string.IsNullOrWhiteSpace(waveCode))
        {
            return [];
        }

        return await businessTaskRepository.ListWaveTaskStatsByWaveCodeAndCreatedTimeRangeAsync(startTimeLocal, endTimeLocal, waveCode.Trim(), cancellationToken);
    }

    /// <summary>
    /// 解析波次备注。
    /// </summary>
    /// <param name="tasks">任务集合。</param>
    /// <returns>波次备注。</returns>
    private static string? ResolveWaveRemark(IReadOnlyList<BusinessTaskWaveTaskStatsRow> tasks)
    {
        return tasks
            .Where(task => !string.IsNullOrWhiteSpace(task.WaveRemark))
            .OrderByDescending(task => task.UpdatedTimeLocal)
            .Select(task => task.WaveRemark!.Trim())
            .FirstOrDefault();
    }

    /// <summary>
    /// 判断是否已分拣。
    /// </summary>
    /// <param name="task">业务任务。</param>
    /// <returns>是否已分拣。</returns>
    private static bool IsSorted(BusinessTaskStatus status)
    {
        return status == BusinessTaskStatus.Dropped || status == BusinessTaskStatus.FeedbackPending;
    }

    /// <summary>
    /// 构建固定顺序的分区统计容器。
    /// </summary>
    /// <returns>分区统计容器。</returns>
    private static Dictionary<string, ZoneAccumulator> BuildZoneAccumulatorMap()
    {
        return new Dictionary<string, ZoneAccumulator>(StringComparer.Ordinal)
        {
            [SplitZone1Code] = new ZoneAccumulator(SplitZone1Code, "拆零1区"),
            [SplitZone2Code] = new ZoneAccumulator(SplitZone2Code, "拆零2区"),
            [SplitZone3Code] = new ZoneAccumulator(SplitZone3Code, "拆零3区"),
            [SplitZone4Code] = new ZoneAccumulator(SplitZone4Code, "拆零4区"),
            [FullCaseCode] = new ZoneAccumulator(FullCaseCode, "整件数据")
        };
    }

    /// <summary>
    /// 解析任务所属分区编码。
    /// </summary>
    /// <param name="task">业务任务。</param>
    /// <returns>分区编码；无法归类返回空值。</returns>
    private string? ResolveTargetZone(BusinessTaskWaveTaskStatsRow task)
    {
        if (task.SourceType == BusinessTaskSourceType.FullCase)
        {
            return FullCaseCode;
        }

        if (task.SourceType != BusinessTaskSourceType.Split)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(task.WorkingArea))
        {
            logger.LogWarning(
                "波次分区统计跳过拆零任务：WorkingArea 为空。TaskCode={TaskCode}, WaveCode={WaveCode}, SourceType={SourceType}",
                task.TaskCode,
                task.WaveCode,
                task.SourceType);
            return null;
        }

        if (!int.TryParse(task.WorkingArea.Trim(), out var workingArea))
        {
            logger.LogWarning(
                "波次分区统计跳过拆零任务：WorkingArea 不是有效整数。TaskCode={TaskCode}, WaveCode={WaveCode}, SourceType={SourceType}, WorkingArea={WorkingArea}",
                task.TaskCode,
                task.WaveCode,
                task.SourceType,
                task.WorkingArea);
            return null;
        }

        // 拆零区域映射固定为: 1→拆零1区、2→拆零2区、3→拆零3区、4→拆零4区；其余值一律跳过。
        return workingArea switch
        {
            1 => SplitZone1Code,
            2 => SplitZone2Code,
            3 => SplitZone3Code,
            4 => SplitZone4Code,
            _ => LogAndSkipInvalidWorkingArea(task)
        };
    }

    /// <summary>
    /// 记录非法工作区域并返回空值。
    /// </summary>
    /// <param name="task">业务任务。</param>
    /// <returns>空值。</returns>
    private string? LogAndSkipInvalidWorkingArea(BusinessTaskWaveTaskStatsRow task)
    {
        logger.LogWarning(
            "波次分区统计跳过拆零任务：WorkingArea 超出允许范围 1~4。TaskCode={TaskCode}, WaveCode={WaveCode}, SourceType={SourceType}, WorkingArea={WorkingArea}",
            task.TaskCode,
            task.WaveCode,
            task.SourceType,
            task.WorkingArea);
        return null;
    }

    /// <summary>
    /// 分区累计统计模型。
    /// </summary>
    /// <param name="zoneCode">分区编码。</param>
    /// <param name="zoneName">分区名称。</param>
    private sealed class ZoneAccumulator(string zoneCode, string zoneName)
    {
        /// <summary>
        /// 分区编码。
        /// </summary>
        public string ZoneCode { get; } = zoneCode;

        /// <summary>
        /// 分区名称。
        /// </summary>
        public string ZoneName { get; } = zoneName;

        /// <summary>
        /// 总件数。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 未分拣件数。
        /// </summary>
        public int UnsortedCount { get; set; }

        /// <summary>
        /// 回流件数。
        /// </summary>
        public int RecirculatedCount { get; set; }

        /// <summary>
        /// 异常件数。
        /// </summary>
        public int ExceptionCount { get; set; }
    }
}
