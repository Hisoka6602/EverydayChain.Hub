using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 定义当前类型。
/// </summary>
[ApiController]
[Route("api/v1/recirculations")]
public sealed class RecirculationController(
    IRecirculationQueryService recirculationQueryService,
    IBusinessTaskReadService businessTaskReadService) : QueryControllerBase
{
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("summary")]
    [ProducesResponseType(typeof(ApiResponse<RecirculationSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecirculationSummaryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RecirculationSummaryResponse>>> QuerySummaryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecirculationSummaryQueryRequest? request,
        [FromQuery] RecirculationSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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
        return Ok(ApiResponse<RecirculationSummaryResponse>.Success(response, "Recirculation summary query succeeded."));
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("details")]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BusinessTaskQueryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BusinessTaskQueryResponse>>> QueryDetailsAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BusinessTaskQueryRequest? request,
        [FromQuery] BusinessTaskQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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

        return Ok(ApiResponse<BusinessTaskQueryResponse>.Success(BuildBusinessTaskResponse(result), "Recirculation detail query succeeded."));
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    [HttpPost("summary/export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportSummaryCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecirculationSummaryQueryRequest? request,
        [FromQuery] RecirculationSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    [HttpPost("summary/export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportSummaryXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecirculationSummaryQueryRequest? request,
        [FromQuery] RecirculationSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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
            "RecirculationSummary",
            ["Chute", "WaveCode", "RecirculatedCount"],
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
    /// 执行当前方法。
    /// </summary>
    private static BusinessTaskQueryResponse BuildBusinessTaskResponse(EverydayChain.Hub.Application.Models.BusinessTaskQueryResult queryResult)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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
    /// 执行当前方法。
    /// </summary>
    private bool TryValidateBusinessTaskRequest(
        BusinessTaskQueryRequest request,
        out DateTime normalizedStart,
        out DateTime normalizedEnd,
        out ActionResult<ApiResponse<BusinessTaskQueryResponse>>? validationResult)
    {
        // 步骤：按既定流程执行当前方法逻辑。
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
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("PageNumber must be between 1 and 100000."));
            return false;
        }

        if (request.PageSize < 1 || request.PageSize > 1000)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("PageSize must be between 1 and 1000."));
            return false;
        }

        if (request.LastCreatedTimeLocal.HasValue != request.LastId.HasValue)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("LastCreatedTimeLocal and LastId must be provided together."));
            return false;
        }

        if (request.LastId.HasValue && request.LastId.Value <= 0)
        {
            validationResult = BadRequest(ApiResponse<BusinessTaskQueryResponse>.Fail("LastId must be greater than 0."));
            return false;
        }

        return true;
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static byte[] BuildUtf8BomCsvBytes(string csvContent)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var preamble = Utf8EncodingWithBom.GetPreamble();
        var contentBytes = Utf8EncodingWithBom.GetBytes(csvContent);
        var bytes = new byte[preamble.Length + contentBytes.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(contentBytes, 0, bytes, preamble.Length, contentBytes.Length);
        return bytes;
    }
}

