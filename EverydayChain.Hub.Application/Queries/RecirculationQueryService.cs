using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using System.Text;

namespace EverydayChain.Hub.Application.Queries;

public sealed class RecirculationQueryService(IBusinessTaskRepository businessTaskRepository) : IRecirculationQueryService
{
    public async Task<RecirculationSummaryQueryResult> QuerySummaryAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        var selectedChuteCode = NormalizeOptionalText(request.ChuteCode);
        var sortOrder = NormalizeSortOrder(request.SortOrder);
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new RecirculationSummaryQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal,
                SelectedChuteCode = selectedChuteCode,
                SortOrder = sortOrder
            };
        }

        var rows = await businessTaskRepository.AggregateRecirculationSummaryAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            selectedChuteCode,
            cancellationToken);
        var orderedRows = (sortOrder == "Least" ? rows.OrderBy(row => row.RecirculatedCount) : rows.OrderByDescending(row => row.RecirculatedCount))
            .ThenBy(row => row.ChuteCode, StringComparer.Ordinal)
            .ThenBy(row => row.WaveCode, StringComparer.Ordinal)
            .Select(row => new RecirculationSummaryRow
            {
                ChuteCode = row.ChuteCode,
                WaveCode = row.WaveCode,
                RecirculatedCount = row.RecirculatedCount
            })
            .ToList();

        return new RecirculationSummaryQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            SelectedChuteCode = selectedChuteCode,
            SortOrder = sortOrder,
            Rows = orderedRows
        };
    }

    public async Task<string> ExportCsvAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
    {
        var result = await QuerySummaryAsync(request, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("Chute,WaveNo,Reflow");
        foreach (var row in result.Rows)
        {
            builder.AppendLine($"{EscapeCsvField(row.ChuteCode)},{EscapeCsvField(row.WaveCode)},{row.RecirculatedCount}");
        }

        return builder.ToString();
    }

    private static string NormalizeSortOrder(string? sortOrder)
    {
        return string.Equals(sortOrder, "Least", StringComparison.OrdinalIgnoreCase) ? "Least" : "Most";
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string EscapeCsvField(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
