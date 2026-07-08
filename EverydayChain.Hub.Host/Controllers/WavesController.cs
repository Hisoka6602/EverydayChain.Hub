using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供波次相关查询与导出接口，用于查看当前波次、波次选项、波次汇总、区域分布与明细列表。
/// </summary>
[ApiController]
[Route("api/v1/waves")]
public sealed class WavesController(IWaveQueryService waveQueryService) : QueryControllerBase
{
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 查询当前自动识别波次，返回最近一次有效扫描对应的波次号、备注、条码与扫描时间。
    /// </summary>
    /// <param name="request">请求体查询条件，指定自动识别使用的时间范围。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>当前自动识别波次结果。</returns>
    [HttpPost("current")]
    [ProducesResponseType(typeof(ApiResponse<CurrentWaveResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CurrentWaveResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CurrentWaveResponse>>> QueryCurrentAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CurrentWaveQueryRequest? request,
        [FromQuery] CurrentWaveQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryCurrentAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<CurrentWaveResponse>.Fail(validationMessage));
        }

        var result = await waveQueryService.QueryCurrentAsync(new EverydayChain.Hub.Application.Models.CurrentWaveQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd
        }, cancellationToken);
        var response = new CurrentWaveResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            WaveCode = result.WaveCode,
            WaveRemark = result.WaveRemark,
            Barcode = result.Barcode,
            ScanTimeLocal = result.ScanTimeLocal
        };
        return Ok(ApiResponse<CurrentWaveResponse>.Success(response, "Current wave query succeeded."));
    }

    /// <summary>
    /// 查询波次下拉选项，返回指定时间范围内可供前端筛选的波次号与备注列表。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次选项结果。</returns>
    [HttpPost("options")]
    [ProducesResponseType(typeof(ApiResponse<WaveOptionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveOptionsResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveOptionsResponse>>> QueryOptionsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveOptionsQueryRequest? request,
        [FromQuery] WaveOptionsQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryOptionsAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<WaveOptionsResponse>.Fail(validationMessage));
        }

        var result = await waveQueryService.QueryOptionsAsync(new EverydayChain.Hub.Application.Models.WaveOptionsQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd
        }, cancellationToken);
        var response = new WaveOptionsResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            WaveOptions = result.WaveOptions
                .Select(item => new WaveOptionItemResponse
                {
                    WaveCode = item.WaveCode,
                    WaveRemark = item.WaveRemark
                })
                .ToList()
        };
        return Ok(ApiResponse<WaveOptionsResponse>.Success(response, "Wave options query succeeded."));
    }

    /// <summary>
    /// 查询单个波次汇总，返回该波次的总件量、待分拣量、进度、回流量与异常量。
    /// </summary>
    /// <param name="request">请求体查询条件，需包含时间范围与波次号。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次汇总结果。</returns>
    [HttpPost("summary")]
    [ProducesResponseType(typeof(ApiResponse<WaveSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveSummaryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveSummaryResponse>>> QuerySummaryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveSummaryQueryRequest? request,
        [FromQuery] WaveSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QuerySummaryAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<WaveSummaryResponse>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<WaveSummaryResponse>.Fail("WaveCode cannot be empty."));
        }

        var result = await waveQueryService.QuerySummaryAsync(new EverydayChain.Hub.Application.Models.WaveSummaryQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        if (result is null)
        {
            return BadRequest(ApiResponse<WaveSummaryResponse>.Fail($"Wave [{resolvedRequest.WaveCode.Trim()}] was not found in the selected range."));
        }

        var response = new WaveSummaryResponse
        {
            WaveCode = result.WaveCode,
            WaveRemark = result.WaveRemark,
            TotalCount = result.TotalCount,
            UnsortedCount = result.UnsortedCount,
            SortedProgressPercent = result.SortedProgressPercent,
            RecirculatedCount = result.RecirculatedCount,
            ExceptionCount = result.ExceptionCount
        };
        return Ok(ApiResponse<WaveSummaryResponse>.Success(response, "Wave summary query succeeded."));
    }

    /// <summary>
    /// 查询波次分区汇总，返回该波次在各区域的总件量、待分拣量、进度、回流量与异常量。
    /// </summary>
    /// <param name="request">请求体查询条件，需包含时间范围与波次号。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次分区汇总结果。</returns>
    [HttpPost("zones")]
    [ProducesResponseType(typeof(ApiResponse<WaveZoneResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveZoneResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveZoneResponse>>> QueryZonesAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveZoneQueryRequest? request,
        [FromQuery] WaveZoneQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryZonesAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<WaveZoneResponse>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<WaveZoneResponse>.Fail("WaveCode cannot be empty."));
        }

        var result = await waveQueryService.QueryZonesAsync(new EverydayChain.Hub.Application.Models.WaveZoneQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        if (result is null)
        {
            return BadRequest(ApiResponse<WaveZoneResponse>.Fail($"Wave [{resolvedRequest.WaveCode.Trim()}] was not found in the selected range."));
        }

        var response = new WaveZoneResponse
        {
            WaveCode = result.WaveCode,
            WaveRemark = result.WaveRemark,
            Zones = result.Zones
                .Select(zone => new WaveZoneSummaryResponse
                {
                    ZoneCode = zone.ZoneCode,
                    ZoneName = zone.ZoneName,
                    TotalCount = zone.TotalCount,
                    UnsortedCount = zone.UnsortedCount,
                    SortedProgressPercent = zone.SortedProgressPercent,
                    RecirculatedCount = zone.RecirculatedCount,
                    ExceptionCount = zone.ExceptionCount
                })
                .ToList()
        };
        return Ok(ApiResponse<WaveZoneResponse>.Success(response, "Wave zone query succeeded."));
    }

    /// <summary>
    /// 导出波次分区汇总 CSV 文件，用于按区域核对波次进度与异常情况。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件流。</returns>
    [HttpPost("zones/export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportZonesCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveZoneQueryRequest? request,
        [FromQuery] WaveZoneQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportZonesCsvAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<object>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<object>.Fail("WaveCode cannot be empty."));
        }

        var csvContent = await waveQueryService.ExportZonesCsvAsync(new EverydayChain.Hub.Application.Models.WaveZoneQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        var fileName = $"wave-zone-detail-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.csv";
        return File(BuildUtf8BomCsvBytes(csvContent), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 查询波次列表，返回指定时间范围内各波次的总量、待分拣量、拆零占比、整件占比、回流量与异常量。
    /// </summary>
    /// <param name="request">请求体查询条件，指定列表统计时间范围。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次列表结果。</returns>
    [HttpPost("list")]
    [ProducesResponseType(typeof(ApiResponse<WaveListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveListResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveListResponse>>> QueryListAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveListQueryRequest? request,
        [FromQuery] WaveListQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryListAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<WaveListResponse>.Fail(validationMessage));
        }

        var result = await waveQueryService.QueryListAsync(new EverydayChain.Hub.Application.Models.WaveListQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd
        }, cancellationToken);
        var response = new WaveListResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            Items = result.Items
                .Select(item => new WaveListItemResponse
                {
                    WaveId = item.WaveCode,
                    Remark = item.WaveRemark,
                    PackageTotal = item.PackageTotal,
                    UnsortedCount = item.UnsortedCount,
                    SplitTotal = item.SplitTotal,
                    FullTotal = item.FullCaseTotal,
                    SplitRatioPercent = item.SplitRatioPercent,
                    FullRatioPercent = item.FullCaseRatioPercent,
                    RecirculatedCount = item.RecirculatedCount,
                    ExceptionCount = item.ExceptionCount,
                    CreatedAt = item.CreatedTimeLocal,
                    Status = item.Status
                })
                .ToList()
        };
        return Ok(ApiResponse<WaveListResponse>.Success(response, "Wave list query succeeded."));
    }

    /// <summary>
    /// 导出波次列表 CSV 文件，便于批量核查多个波次的汇总状态。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件流。</returns>
    [HttpPost("list/export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportListCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveListQueryRequest? request,
        [FromQuery] WaveListQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportListCsvAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<object>.Fail(validationMessage));
        }

        var csvContent = await waveQueryService.ExportListCsvAsync(new EverydayChain.Hub.Application.Models.WaveListQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd
        }, cancellationToken);
        var fileName = $"wave-list-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.csv";
        return File(BuildUtf8BomCsvBytes(csvContent), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 导出波次列表 Excel 文件，便于人工筛选与离线分析。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Excel 文件流。</returns>
    [HttpPost("list/export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportListXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveListQueryRequest? request,
        [FromQuery] WaveListQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportListXlsxAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<object>.Fail(validationMessage));
        }

        var result = await waveQueryService.QueryListAsync(new EverydayChain.Hub.Application.Models.WaveListQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd
        }, cancellationToken);
        var content = SimpleXlsxBuilder.BuildSingleSheet(
            "WaveList",
            ["WaveId", "Remark", "PackageTotal", "UnsortedCount", "SplitTotal", "FullTotal", "SplitRatioPercent", "FullRatioPercent", "RecirculatedCount", "ExceptionCount", "CreatedAt", "Status"],
            result.Items
                .Select(item => (IReadOnlyList<string?>)
                [
                    item.WaveCode,
                    item.WaveRemark,
                    item.PackageTotal.ToString(),
                    item.UnsortedCount.ToString(),
                    item.SplitTotal.ToString(),
                    item.FullCaseTotal.ToString(),
                    item.SplitRatioPercent.ToString("0.##"),
                    item.FullCaseRatioPercent.ToString("0.##"),
                    item.RecirculatedCount.ToString(),
                    item.ExceptionCount.ToString(),
                    item.CreatedTimeLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.Status
                ])
                .ToList());
        var fileName = $"wave-list-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 查询波次明细列表，返回指定波次下每条业务任务的条码、门店、商品、拣货位、格口与状态信息。
    /// </summary>
    /// <param name="request">请求体查询条件，需包含时间范围与波次号。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次明细结果。</returns>
    [HttpPost("details")]
    [ProducesResponseType(typeof(ApiResponse<WaveDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WaveDetailResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WaveDetailResponse>>> QueryDetailsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveDetailQueryRequest? request,
        [FromQuery] WaveDetailQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryDetailsAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<WaveDetailResponse>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<WaveDetailResponse>.Fail("WaveCode cannot be empty."));
        }

        var result = await waveQueryService.QueryDetailsAsync(new EverydayChain.Hub.Application.Models.WaveDetailQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        var response = new WaveDetailResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            WaveCode = result.WaveCode,
            WaveRemark = result.WaveRemark,
            Items = result.Items
                .Select(item => new WaveDetailItemResponse
                {
                    TaskCode = item.TaskCode,
                    WaveCode = item.WaveCode,
                    WaveRemark = item.WaveRemark,
                    SourceType = item.SourceType,
                    WorkingArea = item.WorkingArea,
                    Barcode = item.Barcode,
                    OrderId = item.OrderId,
                    StoreId = item.StoreId,
                    StoreName = item.StoreName,
                    ProductCode = item.ProductCode,
                    PickLocation = item.PickLocation,
                    ChuteCode = item.ChuteCode,
                    Status = item.Status,
                    IsRecirculated = item.IsRecirculated,
                    IsException = item.IsException,
                    ScannedAt = item.ScannedAtLocal,
                    CreatedAt = item.CreatedTimeLocal,
                    UpdatedAt = item.UpdatedTimeLocal
                })
                .ToList()
        };
        return Ok(ApiResponse<WaveDetailResponse>.Success(response, "Wave detail query succeeded."));
    }

    /// <summary>
    /// 导出波次明细 CSV 文件，适用于对单个波次逐件核查任务状态。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件流。</returns>
    [HttpPost("details/export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportDetailsCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveDetailQueryRequest? request,
        [FromQuery] WaveDetailQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportDetailsCsvAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<object>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<object>.Fail("WaveCode cannot be empty."));
        }

        var csvContent = await waveQueryService.ExportDetailsCsvAsync(new EverydayChain.Hub.Application.Models.WaveDetailQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        var fileName = $"wave-detail-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.csv";
        return File(BuildUtf8BomCsvBytes(csvContent), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 导出波次明细 Excel 文件，适用于前端下载后做人工核对与留档。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Excel 文件流。</returns>
    [HttpPost("details/export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportDetailsXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveDetailQueryRequest? request,
        [FromQuery] WaveDetailQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportDetailsXlsxAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<object>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<object>.Fail("WaveCode cannot be empty."));
        }

        var result = await waveQueryService.QueryDetailsAsync(new EverydayChain.Hub.Application.Models.WaveDetailQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        var content = SimpleXlsxBuilder.BuildSingleSheet(
            "WaveDetail",
            ["TaskCode", "WaveCode", "WaveRemark", "SourceType", "WorkingArea", "Barcode", "OrderId", "StoreId", "StoreName", "ProductCode", "PickLocation", "ChuteCode", "Status", "IsRecirculated", "IsException", "ScannedAt", "CreatedAt", "UpdatedAt"],
            result.Items
                .Select(item => (IReadOnlyList<string?>)
                [
                    item.TaskCode,
                    item.WaveCode,
                    item.WaveRemark,
                    item.SourceType,
                    item.WorkingArea,
                    item.Barcode,
                    item.OrderId,
                    item.StoreId,
                    item.StoreName,
                    item.ProductCode,
                    item.PickLocation,
                    item.ChuteCode,
                    item.Status,
                    item.IsRecirculated.ToString(),
                    item.IsException.ToString(),
                    item.ScannedAtLocal?.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.CreatedTimeLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.UpdatedTimeLocal.ToString("yyyy-MM-dd HH:mm:ss")
                ])
                .ToList());
        var fileName = $"wave-detail-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 导出波次分区汇总 Excel 文件，适用于区域级运营复盘。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Excel 文件流。</returns>
    [HttpPost("zones/export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportZonesXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WaveZoneQueryRequest? request,
        [FromQuery] WaveZoneQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportZonesXlsxAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<object>.Fail(validationMessage));
        }

        if (string.IsNullOrWhiteSpace(resolvedRequest.WaveCode))
        {
            return BadRequest(ApiResponse<object>.Fail("WaveCode cannot be empty."));
        }

        var result = await waveQueryService.QueryZonesAsync(new EverydayChain.Hub.Application.Models.WaveZoneQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode.Trim()
        }, cancellationToken);
        var zones = result?.Zones ?? [];
        var content = SimpleXlsxBuilder.BuildSingleSheet(
            "WaveZones",
            ["ZoneName", "TotalCount", "PendingCount", "ProgressPercent", "RecirculatedCount", "ExceptionCount"],
            zones
                .Select(zone => (IReadOnlyList<string?>)
                [
                    zone.ZoneName,
                    zone.TotalCount.ToString(),
                    zone.UnsortedCount.ToString(),
                    zone.SortedProgressPercent.ToString("0.##"),
                    zone.RecirculatedCount.ToString(),
                    zone.ExceptionCount.ToString()
                ])
                .ToList());
        var fileName = $"wave-zone-detail-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static byte[] BuildUtf8BomCsvBytes(string csvContent)
    {
        var preamble = Utf8EncodingWithBom.GetPreamble();
        var contentBytes = Utf8EncodingWithBom.GetBytes(csvContent);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return bytes;
    }
}

