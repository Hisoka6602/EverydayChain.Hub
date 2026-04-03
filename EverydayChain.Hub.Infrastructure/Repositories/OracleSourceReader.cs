using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Repositories;
using EverydayChain.Hub.SharedKernel.Utilities;
using EverydayChain.Hub.Domain.Sync;

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
        EnsureReadOnlyRequest(request.SourceSchema, request.SourceTable);
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
            .Select(row => SyncColumnFilter.FilterExcludedColumns(row, request.NormalizedExcludedColumns))
            .ToList();

        return Task.FromResult(new SyncReadResult
        {
            Rows = filteredRows,
        });
    }

    /// <inheritdoc/>
    public Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct)
    {
        EnsureReadOnlyRequest(request.SourceSchema, request.SourceTable);
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

    /// <summary>
    /// 校验源端只读请求对象名，阻断 DDL/DML 注入风险。
    /// </summary>
    /// <param name="sourceSchema">源端 Schema。</param>
    /// <param name="sourceTable">源端表名。</param>
    /// <exception cref="InvalidOperationException">当对象名包含危险字符时抛出。</exception>
    private static void EnsureReadOnlyRequest(string sourceSchema, string sourceTable)
    {
        ValidateSafeIdentifier(sourceSchema, nameof(sourceSchema));
        ValidateSafeIdentifier(sourceTable, nameof(sourceTable));
    }

    /// <summary>
    /// 校验对象名仅包含安全字符。
    /// </summary>
    /// <param name="identifier">对象名。</param>
    /// <param name="fieldName">字段名。</param>
    /// <exception cref="InvalidOperationException">当对象名非法时抛出。</exception>
    private static void ValidateSafeIdentifier(string identifier, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException($"{fieldName} 不能为空。");
        }

        if (!identifier.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            throw new InvalidOperationException($"{fieldName} 包含非法字符，仅允许字母、数字、下划线。");
        }
    }
}
