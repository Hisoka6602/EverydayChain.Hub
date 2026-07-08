using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Host.Contracts.Requests;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.SharedKernel.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace EverydayChain.Hub.Host.Controllers;

/// <summary>
/// 提供箱子追踪查询与导出接口，用于按时间范围回溯扫描条码的识别、匹配、落格与任务明细。
/// 这里的箱号追踪延续前端页面命名，但查询主键实际使用扫描日志中的条码值，不改变既有分拣机联调语义。
/// </summary>
[ApiController]
[Route("api/v1/box-tracking")]
public sealed class BoxTrackingController(IBoxTrackingQueryService boxTrackingQueryService) : QueryControllerBase
{
    private static readonly UTF8Encoding Utf8EncodingWithBom = new(true);

    /// <summary>
    /// 查询箱子追踪明细，返回指定时间段内的扫描条码记录、订单信息、门店信息、商品信息与落格状态。
    /// </summary>
    /// <param name="request">请求体查询条件。字段名 boxId 沿用前端页面命名，但实际筛选的是扫描日志中的 Barcode。</param>
    /// <param name="queryRequest">查询字符串查询条件，字段语义与请求体一致，便于在浏览器中直接调试接口。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>箱号追踪分页结果。</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<BoxTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BoxTrackingResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BoxTrackingResponse>>> QueryAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BoxTrackingQueryRequest? request,
        [FromQuery] BoxTrackingQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return BadRequest(ApiResponse<BoxTrackingResponse>.Fail(validationMessage));
        }

        if (resolvedRequest.PageNumber < 1 || resolvedRequest.PageNumber > 100000)
        {
            return BadRequest(ApiResponse<BoxTrackingResponse>.Fail("PageNumber must be between 1 and 100000."));
        }

        if (resolvedRequest.PageSize < 1 || resolvedRequest.PageSize > 1000)
        {
            return BadRequest(ApiResponse<BoxTrackingResponse>.Fail("PageSize must be between 1 and 1000."));
        }

        var result = await boxTrackingQueryService.QueryAsync(BuildQueryRequest(resolvedRequest, normalizedStart, normalizedEnd), cancellationToken);
        return Ok(ApiResponse<BoxTrackingResponse>.Success(BuildResponse(result), "Box tracking query succeeded."));
    }

    /// <summary>
    /// 导出箱子追踪 CSV 文件，字段与箱子追踪明细列表一致，便于运营排查与离线核对。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>CSV 文件流。</returns>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportCsvAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BoxTrackingQueryRequest? request,
        [FromQuery] BoxTrackingQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportCsvAsync 方法的核心处理流程。
        var rows = await QueryAllRowsAsync(request, queryRequest, cancellationToken);
        if (rows.Result is not null)
        {
            return rows.Result;
        }

        var csv = BuildCsv(rows.Value!);
        var fileName = $"box-tracking-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.csv";
        return File(BuildUtf8BomCsvBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// 导出箱子追踪 Excel 文件，字段与箱子追踪明细列表一致，适合做人工筛选与二次分析。
    /// </summary>
    /// <param name="request">请求体查询条件。</param>
    /// <param name="queryRequest">查询字符串查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Excel 文件流。</returns>
    [HttpPost("export/xlsx")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportXlsxAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BoxTrackingQueryRequest? request,
        [FromQuery] BoxTrackingQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 ExportXlsxAsync 方法的核心处理流程。
        var rows = await QueryAllRowsAsync(request, queryRequest, cancellationToken);
        if (rows.Result is not null)
        {
            return rows.Result;
        }

        var content = SimpleXlsxBuilder.BuildSingleSheet(
            "BoxTracking",
            ["OrderId", "BoxId", "StoreId", "StoreName", "ProductCode", "PickLocation", "Scanner", "ScannedAt", "Chute", "Status"],
            rows.Value!
                .Select(item => (IReadOnlyList<string?>)
                [
                    item.OrderId,
                    item.BoxId,
                    item.StoreId,
                    item.StoreName,
                    item.ProductCode,
                    item.PickLocation,
                    item.Scanner,
                    item.ScannedAtLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                    item.Chute,
                    item.Status
                ])
                .ToList());
        var fileName = $"box-tracking-{DateTimeOffset.Now.LocalDateTime:yyyyMMddHHmmss}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// 执行 QueryAllRowsAsync 方法。
    /// </summary>
    private async Task<(IReadOnlyList<EverydayChain.Hub.Application.Models.BoxTrackingItem>? Value, ActionResult? Result)> QueryAllRowsAsync(
        BoxTrackingQueryRequest? request,
        BoxTrackingQueryRequest? queryRequest,
        CancellationToken cancellationToken)
    {
        // 步骤：执行 QueryAllRowsAsync 方法的核心处理流程。
        var resolvedRequest = ResolveRequest(request, queryRequest);
        if (!LocalTimeRangeValidator.TryNormalizeRequiredRange(
                resolvedRequest.StartTimeLocal,
                resolvedRequest.EndTimeLocal,
                out var normalizedStart,
                out var normalizedEnd,
                out var validationMessage))
        {
            return (null, BadRequest(ApiResponse<object>.Fail(validationMessage)));
        }

        var items = await boxTrackingQueryService.QueryAllAsync(BuildQueryRequest(resolvedRequest, normalizedStart, normalizedEnd), cancellationToken);
        return (items, null);
    }

    /// <summary>
    /// 执行 BuildQueryRequest 方法。
    /// </summary>
    private static EverydayChain.Hub.Application.Models.BoxTrackingQueryRequest BuildQueryRequest(
        BoxTrackingQueryRequest request,
        DateTime normalizedStart,
        DateTime normalizedEnd)
    {
        // 步骤：执行 BuildQueryRequest 方法的核心处理流程。
        // 步骤：仅做字段转译，不改变 boxId 对应扫描日志 Barcode 的既有查询语义。
        return new EverydayChain.Hub.Application.Models.BoxTrackingQueryRequest
        {
            StartTimeLocal = normalizedStart,
            EndTimeLocal = normalizedEnd,
            BoxId = request.BoxId,
            OrderId = request.OrderId,
            StoreId = request.StoreId,
            Scanner = request.Scanner,
            ChuteCode = request.ChuteCode,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static BoxTrackingResponse BuildResponse(EverydayChain.Hub.Application.Models.BoxTrackingQueryResult result)
    {
        return new BoxTrackingResponse
        {
            StartTimeLocal = result.StartTimeLocal,
            EndTimeLocal = result.EndTimeLocal,
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            Items = result.Items
                .Select(item => new BoxTrackingItemResponse
                {
                    BoxId = item.BoxId,
                    TaskCode = item.TaskCode,
                    WaveCode = item.WaveCode,
                    OrderId = item.OrderId,
                    StoreId = item.StoreId,
                    StoreName = item.StoreName,
                    ProductCode = item.ProductCode,
                    PickLocation = item.PickLocation,
                    Scanner = item.Scanner,
                    ScannedAt = item.ScannedAtLocal,
                    Chute = item.Chute,
                    Status = item.Status,
                    IsMatched = item.IsMatched,
                    FailureReason = item.FailureReason
                })
                .ToList()
        };
    }

    private static string BuildCsv(IReadOnlyList<EverydayChain.Hub.Application.Models.BoxTrackingItem> items)
    {
        var builder = new StringBuilder();
        // 步骤：导出列名继续沿用 BoxId，避免影响既有前端和人工使用习惯，但其值实际为扫描条码。
        builder.AppendLine("OrderId,BoxId,StoreId,StoreName,ProductCode,PickLocation,Scanner,ScannedAt,Chute,Status");
        foreach (var item in items)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsvField(item.OrderId),
                EscapeCsvField(item.BoxId),
                EscapeCsvField(item.StoreId),
                EscapeCsvField(item.StoreName),
                EscapeCsvField(item.ProductCode),
                EscapeCsvField(item.PickLocation),
                EscapeCsvField(item.Scanner),
                EscapeCsvField(item.ScannedAtLocal.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsvField(item.Chute),
                EscapeCsvField(item.Status)));
        }

        return builder.ToString();
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

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

