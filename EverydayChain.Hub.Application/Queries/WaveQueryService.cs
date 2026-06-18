using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EverydayChain.Hub.Application.Queries;

public sealed class WaveQueryService(
    IBusinessTaskRepository businessTaskRepository,
    ILogger<WaveQueryService> logger) : IWaveQueryService
{
    private const string SplitZone1Code = "SplitZone1";
    private const string SplitZone2Code = "SplitZone2";
    private const string SplitZone3Code = "SplitZone3";
    private const string SplitZone4Code = "SplitZone4";
    private const string FullCaseCode = "FullCase";

    private static readonly IReadOnlyList<string> ZoneOutputOrder =
    [
        SplitZone1Code,
        SplitZone2Code,
        SplitZone3Code,
        SplitZone4Code,
        FullCaseCode
    ];

    private readonly BusinessTaskQueryPolicy _queryPolicy = new();

    public async Task<CurrentWaveQueryResult> QueryCurrentAsync(CurrentWaveQueryRequest request, CancellationToken cancellationToken)
    {
        var result = new CurrentWaveQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal
        };
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return result;
        }

        var latestTask = await businessTaskRepository.FindLatestScannedWithWaveByCreatedTimeRangeAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            cancellationToken);
        if (latestTask is null)
        {
            return result;
        }

        result.WaveCode = NormalizeOptionalText(latestTask.WaveCode);
        result.WaveRemark = NormalizeOptionalText(latestTask.WaveRemark);
        result.Barcode = NormalizeOptionalText(latestTask.Barcode);
        result.ScanTimeLocal = latestTask.ScannedAtLocal;
        return result;
    }

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

    public async Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        var taskStats = await FindWaveTaskStatsAsync(request.StartTimeLocal, request.EndTimeLocal, request.WaveCode, cancellationToken);
        if (taskStats.Count == 0)
        {
            return null;
        }

        var totalCount = taskStats.Count;
        var unsortedCount = taskStats.Count(task => !IsSorted(task.Status));
        var sortedCount = totalCount - unsortedCount;
        return new WaveSummaryQueryResult
        {
            WaveCode = request.WaveCode.Trim(),
            WaveRemark = ResolveWaveRemark(taskStats),
            TotalCount = totalCount,
            UnsortedCount = unsortedCount,
            SortedProgressPercent = _queryPolicy.CalculatePercent(sortedCount, totalCount),
            RecirculatedCount = taskStats.Count(task => _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode)),
            ExceptionCount = taskStats.Count(task => task.IsException || task.Status == BusinessTaskStatus.Exception)
        };
    }

    public async Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
    {
        var taskStats = await FindWaveTaskStatsAsync(request.StartTimeLocal, request.EndTimeLocal, request.WaveCode, cancellationToken);
        if (taskStats.Count == 0)
        {
            return null;
        }

        var zoneMap = BuildZoneAccumulatorMap();
        foreach (var task in taskStats)
        {
            var targetZone = ResolveTargetZone(task);
            if (targetZone is null || !zoneMap.TryGetValue(targetZone, out var zone))
            {
                continue;
            }

            zone.TotalCount++;
            if (!IsSorted(task.Status))
            {
                zone.UnsortedCount++;
            }

            if (_queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode))
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
                    SortedProgressPercent = _queryPolicy.CalculatePercent(sortedCount, zone.TotalCount),
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

    public async Task<string> ExportZonesCsvAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await QueryZonesAsync(request, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("ZoneName,TotalCount,PendingCount,ProgressPercent,RecirculatedCount,ExceptionCount");
        if (result is null)
        {
            return builder.ToString();
        }

        foreach (var zone in result.Zones)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsvField(zone.ZoneName),
                zone.TotalCount,
                zone.UnsortedCount,
                zone.SortedProgressPercent,
                zone.RecirculatedCount,
                zone.ExceptionCount));
        }

        return builder.ToString();
    }

    public async Task<WaveListQueryResult> QueryListAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new WaveListQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        var waveRows = await businessTaskRepository.AggregateWaveDashboardAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
        var optionMap = (await businessTaskRepository.ListWaveOptionsByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken))
            .ToDictionary(item => item.WaveCode, item => item.WaveRemark, StringComparer.OrdinalIgnoreCase);

        var items = waveRows
            .OrderBy(row => row.WaveCode, StringComparer.Ordinal)
            .Select(row => new WaveListItem
            {
                WaveCode = row.WaveCode,
                WaveRemark = optionMap.TryGetValue(row.WaveCode, out var waveRemark) ? waveRemark : null,
                PackageTotal = row.TotalCount,
                SplitTotal = row.SplitTotalCount,
                FullCaseTotal = row.FullCaseTotalCount,
                CreatedTimeLocal = row.EarliestCreatedTimeLocal,
                Status = ResolveWaveStatus(row)
            })
            .ToList();

        return new WaveListQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            Items = items
        };
    }

    public async Task<string> ExportListCsvAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await QueryListAsync(request, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("WaveId,Remark,PackageTotal,SplitTotal,FullTotal,CreatedAt,Status");
        foreach (var item in result.Items)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsvField(item.WaveCode),
                EscapeCsvField(item.WaveRemark ?? string.Empty),
                item.PackageTotal,
                item.SplitTotal,
                item.FullCaseTotal,
                item.CreatedTimeLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                EscapeCsvField(item.Status)));
        }

        return builder.ToString();
    }

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

    private static string? ResolveWaveRemark(IReadOnlyList<BusinessTaskWaveTaskStatsRow> tasks)
    {
        return tasks
            .Where(task => !string.IsNullOrWhiteSpace(task.WaveRemark))
            .OrderByDescending(task => task.UpdatedTimeLocal)
            .Select(task => task.WaveRemark!.Trim())
            .FirstOrDefault();
    }

    private static bool IsSorted(BusinessTaskStatus status)
    {
        return status == BusinessTaskStatus.Dropped || status == BusinessTaskStatus.FeedbackPending;
    }

    private static string ResolveWaveStatus(BusinessTaskWaveAggregateRow row)
    {
        if (row.ExceptionCount > 0)
        {
            return "ExceptionPending";
        }

        if (row.UnsortedCount > 0)
        {
            return "Sorting";
        }

        return "Completed";
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Dictionary<string, ZoneAccumulator> BuildZoneAccumulatorMap()
    {
        return new Dictionary<string, ZoneAccumulator>(StringComparer.Ordinal)
        {
            [SplitZone1Code] = new ZoneAccumulator(SplitZone1Code, "Split Zone 1"),
            [SplitZone2Code] = new ZoneAccumulator(SplitZone2Code, "Split Zone 2"),
            [SplitZone3Code] = new ZoneAccumulator(SplitZone3Code, "Split Zone 3"),
            [SplitZone4Code] = new ZoneAccumulator(SplitZone4Code, "Split Zone 4"),
            [FullCaseCode] = new ZoneAccumulator(FullCaseCode, "Full Case")
        };
    }

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
                "Wave zone statistics skipped a split task because WorkingArea is empty. TaskCode={TaskCode}, WaveCode={WaveCode}, SourceType={SourceType}",
                task.TaskCode,
                task.WaveCode,
                task.SourceType);
            return null;
        }

        if (!int.TryParse(task.WorkingArea.Trim(), out var workingArea))
        {
            logger.LogWarning(
                "Wave zone statistics skipped a split task because WorkingArea is invalid. TaskCode={TaskCode}, WaveCode={WaveCode}, SourceType={SourceType}, WorkingArea={WorkingArea}",
                task.TaskCode,
                task.WaveCode,
                task.SourceType,
                task.WorkingArea);
            return null;
        }

        return workingArea switch
        {
            1 => SplitZone1Code,
            2 => SplitZone2Code,
            3 => SplitZone3Code,
            4 => SplitZone4Code,
            _ => LogAndSkipInvalidWorkingArea(task)
        };
    }

    private string? LogAndSkipInvalidWorkingArea(BusinessTaskWaveTaskStatsRow task)
    {
        logger.LogWarning(
            "Wave zone statistics skipped a split task because WorkingArea is out of range. TaskCode={TaskCode}, WaveCode={WaveCode}, SourceType={SourceType}, WorkingArea={WorkingArea}",
            task.TaskCode,
            task.WaveCode,
            task.SourceType,
            task.WorkingArea);
        return null;
    }

    private static string EscapeCsvField(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private sealed class ZoneAccumulator(string zoneCode, string zoneName)
    {
        public string ZoneCode { get; } = zoneCode;

        public string ZoneName { get; } = zoneName;

        public int TotalCount { get; set; }

        public int UnsortedCount { get; set; }

        public int RecirculatedCount { get; set; }

        public int ExceptionCount { get; set; }
    }
}
