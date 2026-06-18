using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

[ApiController]
[Route("api/v1/recirculations")]
public sealed class RecirculationController(IRecirculationQueryService recirculationQueryService) : QueryControllerBase
{
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    [HttpPost("summary")]
    [ProducesResponseType(typeof(ApiResponse<RecirculationSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecirculationSummaryResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RecirculationSummaryResponse>>> QuerySummaryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecirculationSummaryQueryRequest? request,
        [FromQuery] RecirculationSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
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

    [HttpPost("summary/export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportSummaryCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RecirculationSummaryQueryRequest? request,
        [FromQuery] RecirculationSummaryQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
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
