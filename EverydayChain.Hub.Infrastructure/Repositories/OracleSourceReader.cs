using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;

namespace EverydayChain.Hub.Infrastructure.Repositories;

/// <summary>
/// Oracle 源端读取器基础实现（当前使用内存集合模拟分页读取）。
/// </summary>
public class OracleSourceReader : IOracleSourceReader
{
    /// <summary>源数据缓存（按表编码）。</summary>
    private static readonly Dictionary<string, List<IReadOnlyDictionary<string, object?>>> SourceData = BuildSeedData();

    /// <inheritdoc/>
    public Task<SyncReadResult> ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct)
    {
        if (request.PageNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageNo), request.PageNo, "PageNo 必须大于 0。");
        }

        if (request.PageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageSize), request.PageSize, "PageSize 必须大于 0。");
        }

        if (!SourceData.TryGetValue(request.TableCode, out var rows))
        {
            return Task.FromResult(new SyncReadResult());
        }

        var filteredRows = rows
            .Where(row => row.TryGetValue(request.CursorColumn, out var value)
                          && value is DateTime cursorLocal
                          && cursorLocal > request.Window.WindowStartLocal
                          && cursorLocal <= request.Window.WindowEndLocal)
            .OrderBy(row => (DateTime)row[request.CursorColumn]!)
            .ThenBy(row => BuildStableKey(row, request.UniqueKeys))
            .Skip((request.PageNo - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(row => FilterExcludedColumns(row, request.ExcludedColumns))
            .ToList();

        return Task.FromResult(new SyncReadResult
        {
            Rows = filteredRows,
        });
    }

    /// <inheritdoc/>
    public Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!SourceData.TryGetValue(request.TableCode, out var rows))
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        var keySet = rows
            .Where(row => row.TryGetValue(request.CursorColumn, out var value)
                          && value is DateTime cursorLocal
                          && cursorLocal > request.Window.WindowStartLocal
                          && cursorLocal <= request.Window.WindowEndLocal)
            .Select(row => BuildStableKey(row, request.UniqueKeys))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlySet<string>>(keySet);
    }

    /// <summary>
    /// 构造稳定排序键。
    /// </summary>
    /// <param name="row">数据行。</param>
    /// <param name="uniqueKeys">唯一键集合。</param>
    /// <returns>稳定键。</returns>
    private static string BuildStableKey(IReadOnlyDictionary<string, object?> row, IReadOnlyList<string> uniqueKeys)
    {
        if (uniqueKeys.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("|", uniqueKeys.Select(key => row.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty));
    }

    /// <summary>
    /// 过滤行中的排除列。
    /// </summary>
    /// <param name="row">数据行。</param>
    /// <param name="excludedColumns">排除列集合。</param>
    /// <returns>过滤后的数据行。</returns>
    private static IReadOnlyDictionary<string, object?> FilterExcludedColumns(IReadOnlyDictionary<string, object?> row, IReadOnlyList<string> excludedColumns)
    {
        if (excludedColumns.Count == 0)
        {
            return new Dictionary<string, object?>(row);
        }

        var excludedColumnSet = excludedColumns
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (excludedColumnSet.Count == 0)
        {
            return new Dictionary<string, object?>(row);
        }

        var filtered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in row)
        {
            if (!excludedColumnSet.Contains(pair.Key))
            {
                filtered[pair.Key] = pair.Value;
            }
        }

        return filtered;
    }

    /// <summary>
    /// 构建演示源数据。
    /// </summary>
    /// <returns>源数据字典。</returns>
    private static Dictionary<string, List<IReadOnlyDictionary<string, object?>>> BuildSeedData()
    {
        var nowLocal = DateTime.Now;
        var records = new List<IReadOnlyDictionary<string, object?>>();
        for (var i = 1; i <= 30; i++)
        {
            records.Add(new Dictionary<string, object?>
            {
                ["BusinessNo"] = $"ORDER-{i:0000}",
                ["Status"] = i % 2 == 0 ? "Processing" : "Created",
                ["Payload"] = $"同步演示数据-{i}",
                ["LastModifiedTime"] = nowLocal.AddMinutes(-120 + (i * 2)),
            });
        }

        return new Dictionary<string, List<IReadOnlyDictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["SortingTaskTrace"] = records,
        };
    }
}
