using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供回流汇总、回流明细与导出接口，用于分析不同格口和波次下的回流情况。
/// </summary>
[ApiController]
[Route("api/v1/recirculations")]
public sealed class RecirculationController(
    IRecirculationQueryService recirculationQueryService,
    IBusinessTaskReadService businessTaskReadService) : QueryControllerBase
{
    /// <summary>
    /// 生成回流导出 CSV 时需要的 UTF-8 BOM 编码。
    /// </summary>
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 查询回流汇总，返回指定时间范围内各格口、各波次的回流次数统计。
    /// </summary>
    /// <param name="request">请求体查询条件，支持按时间范围、格口与排序方式筛选。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>回流汇总结果。</returns>
    [HttpPost("summary")]
    [ProducesResponseType(typeof(ApiResponse<RecirculationSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecirculationSummaryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RecirculationSummaryResponse>>> QuerySummaryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecirculationSummaryQueryRequest? request,
        [FromQuery] RecirculationSummaryQueryRequest? queryRequest,
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
            return BadRequest(ApiResponse<RecirculationSummaryResponse>.Fail(validationMessage));
        }

        var result = await recirculationQueryService.QuerySummaryAsync(new EverydayChain.Hub.Application.Models.RecirculationSummaryQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            ChuteCode = resolvedRequest.ChuteCode,
            SortOrder = resolvedRequest.SortOrder
        }, cancellationToken);
        var response = new RecirculationSummaryResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            SelectedChuteCode = result.SelectedChuteCode,
            SortOrder = result.SortOrder,
            Rows = result.Rows
                .Select(row => new RecirculationSummaryRowResponse
                {
                    Chute = row.ChuteCode,
                    WaveNo = row.WaveCode,
                    Reflow = row.RecirculatedCount
                })
                .ToList()
        };
        return Ok(ApiResponse<RecirculationSummaryResponse>.Success(response, "回流汇总查询成功。"));
    }

    /// <summary>
    /// 查询回流明细，返回具体发生回流的业务任务列表，包含条码、波次、格口与业务扩展字段。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>回流明细分页结果。</returns>
    [HttpPost("details")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryDetailsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskQueryRequest? request,
        [FromQuery] BusinessTaskQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryDetailsAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!TryValidateBusinessTaskRequest(resolvedRequest, out var normalizedStart, out var normalizedEnd, out var badRequest))
        {
            return badRequest!;
        }

        var result = await businessTaskReadService.QueryRecirculationsAsync(new EverydayChain.Hub.Application.Models.BusinessTaskQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            WaveCode = resolvedRequest.WaveCode,
            Barcode = resolvedRequest.Barcode,
            DockCode = resolvedRequest.DockCode,
            ChuteCode = resolvedRequest.ChuteCode,
            PageNumber = resolvedRequest.PageNumber,
            PageSize = resolvedRequest.PageSize,
            LastCreatedTimeLocal = resolvedRequest.LastCreatedTimeLocal,
            LastId = resolvedRequest.LastId
        }, cancellationToken);

        return Ok(ApiResponse<BusinessTaskQueryResponse>.Success(BuildBusinessTaskResponse(result), "回流明细查询成功。"));
    }

    /// <summary>
    /// 导出回流汇总 CSV 文件，适用于按格口和波次核对回流数量。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件流。</returns>
    [HttpPost("summary/export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportSummaryCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecirculationSummaryQueryRequest? request,
        [FromQuery] RecirculationSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportSummaryCsvAsync 方法的核心处理流程。
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

        var csvContent = await recirculationQueryService.ExportCsvAsync(new EverydayChain.Hub.Application.Models.RecirculationSummaryQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            ChuteCode = resolvedRequest.ChuteCode,
            SortOrder = resolvedRequest.SortOrder
        }, cancellationToken);
        var fileName = $"recirculation-summary-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.csv";
        return File(BuildUtf8BomCsvBytes(csvContent), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 导出回流汇总 Excel 文件，适用于报表归档和人工分析。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Excel 文件流。</returns>
    [HttpPost("summary/export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportSummaryXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecirculationSummaryQueryRequest? request,
        [FromQuery] RecirculationSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportSummaryXlsxAsync 方法的核心处理流程。
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

        var result = await recirculationQueryService.QuerySummaryAsync(new EverydayChain.Hub.Application.Models.RecirculationSummaryQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            ChuteCode = resolvedRequest.ChuteCode,
            SortOrder = resolvedRequest.SortOrder
        }, cancellationToken);
        var content = SimpleXlsxBuilder.BuildSingleSheet(
            "回流汇总",
            ["格口", "波次号", "回流数"],
            result.Rows
                .Select(row => (IReadOnlyList<string?>)
                [
                    row.ChuteCode,
                    row.WaveCode,
                    row.RecirculatedCount.ToString()
                ])
                .ToList());
        var fileName = $"recirculation-summary-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 执行 BuildBusinessTaskResponse 方法。
    /// </summary>
    private static BusinessTaskQueryResponse BuildBusinessTaskResponse(EverydayChain.Hub.Application.Models.BusinessTaskQueryResult queryResult)
    {
        // 步骤：执行 BuildBusinessTaskResponse 方法的核心处理流程。
        return new BusinessTaskQueryResponse
        {
            TotalCount = queryResult.TotalCount,
            PageNumber = queryResult.PageNumber,
            PageSize = queryResult.PageSize,
            HasMore = queryResult.HasMore,
            NextLastCreatedTimeLocal = queryResult.NextLastCreatedTimeLocal,
            NextLastId = queryResult.NextLastId,
            PaginationMode = queryResult.PaginationMode,
            Items = queryResult.Items
                .Select(item => new BusinessTaskItemResponse
                {
                    TaskCode = item.TaskCode,
                    Barcode = item.Barcode,
                    WaveCode = item.WaveCode,
                    SourceType = (int)item.SourceType,
                    Status = (int)item.Status,
                    TargetChuteCode = item.TargetChuteCode,
                    ActualChuteCode = item.ActualChuteCode,
                    DockCode = item.DockCode,
                    IsRecirculated = item.IsRecirculated,
                    IsException = item.IsException,
                    CreatedTimeLocal = item.CreatedTimeLocal,
                    OrderId = item.OrderId,
                    StoreId = item.StoreId,
                    StoreName = item.StoreName,
                    ProductCode = item.ProductCode,
                    PickLocation = item.PickLocation
                })
                .ToList()
        };
    }

    /// <summary>
    /// 执行 TryValidateBusinessTaskRequest 方法。
    /// </summary>
    private bool TryValidateBusinessTaskRequest(
        BusinessTaskQueryRequest request,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out ActionResult<ApiResponse<BusinessTaskQueryResponse>>? validationResult)
    {
        // 步骤：执行 TryValidateBusinessTaskRequest 方法的核心处理流程。
        normalizedStart = default;
        normalizedEnd = default;
        validationResult = null;

        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(request.StartTimeLocal, request.EndTimeLocal, out normalizedStart, out normalizedEnd, out var validationMessage))
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail(validationMessage));
            return false;
        }

        if (request.PageNumber < 1 || request.PageNumber > 100000)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("页码范围必须在 1 到 100000 之间。"));
            return false;
        }

        if (request.PageSize < 1 || request.PageSize > 1000)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("页大小范围必须在 1 到 1000 之间。"));
            return false;
        }

        if (request.LastCreatedTimeLocal.HasValue != request.LastId.HasValue)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("游标分页参数 LastCreatedTimeLocal 与 LastId 必须同时传入或同时为空。"));
            return false;
        }

        if (request.LastId.HasValue && request.LastId.Value <= 0)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("游标分页参数 LastId 必须大于 0。"));
            return false;
        }

        return true;
    }

    /// <summary>
    /// 执行 BuildUtf8BomCsvBytes 方法。
    /// </summary>
    private static byte[] BuildUtf8BomCsvBytes(string csvContent)
    {
        // 步骤：执行 BuildUtf8BomCsvBytes 方法的核心处理流程。
        var preamble = Utf8EncodingWithBom.GetPreamble();
        var contentBytes = Utf8EncodingWithBom.GetBytes(csvContent);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return bytes;
    }
}

