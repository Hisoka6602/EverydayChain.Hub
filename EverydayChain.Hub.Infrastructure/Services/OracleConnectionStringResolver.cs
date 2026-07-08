using Oracle.ManagedDataAccess.Client;
using EverydayChain.Hub.Domain.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 OracleConnectionStringResolver 类型。
/// </summary>
internal static class OracleConnectionStringResolver {

    /// <summary>
    /// 执行 BuildEffectiveConnectionString 方法。
    /// </summary>
    public static string BuildEffectiveConnectionString(OracleOptions options) {
        // 步骤：执行 BuildEffectiveConnectionString 方法的核心处理流程。
        if (string.IsNullOrWhiteSpace(options.ConnectionString)) {
            throw new InvalidOperationException("Oracle.ConnectionString 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(options.Database)) {
            return options.ConnectionString;
        }

        var builder = new OracleConnectionStringBuilder(options.ConnectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)) {
            throw new InvalidOperationException("Oracle.ConnectionString 缺少 Data Source，无法应用 Oracle.Database。");
        }

        builder.DataSource = OverrideOracleDatabase(builder.DataSource, options.Database, options.DatabaseMode);
        return builder.ConnectionString;
    }

    /// <summary>
    /// 执行 OverrideOracleDatabase 方法。
    /// </summary>
    private static string OverrideOracleDatabase(string dataSource, string database, string databaseMode) {
        // 步骤：执行 OverrideOracleDatabase 方法的核心处理流程。
        var trimmedDataSource = dataSource.Trim();
        var trimmedDatabase = database.Trim();
        var normalizedMode = NormalizeDatabaseMode(databaseMode);
        if (string.IsNullOrWhiteSpace(trimmedDatabase)) {
            return trimmedDataSource;
        }

        if (trimmedDataSource.StartsWith('(')) {
            throw new InvalidOperationException("Oracle.ConnectionString 使用复杂 Data Source 描述符时，不支持通过 Oracle.Database 覆写库名。请直接在 ConnectionString 的 Data Source 描述符中指定 SERVICE_NAME 或 SID，或改用 EZCONNECT 格式（例如：主机:端口/库名）。");
        }

        var slashIndex = trimmedDataSource.LastIndexOf('/');
        if (slashIndex >= 0) {
            return $"{trimmedDataSource[..slashIndex]}/{trimmedDatabase}";
        }

        var colonCount = trimmedDataSource.Count(ch => ch == ':');
        if (colonCount >= 2) {
            var lastColonIndex = trimmedDataSource.LastIndexOf(':');
            return $"{trimmedDataSource[..lastColonIndex]}:{trimmedDatabase}";
        }

        if (normalizedMode == "Sid") {
            return $"{trimmedDataSource}:{trimmedDatabase}";
        }

        return $"{trimmedDataSource}/{trimmedDatabase}";
    }

    /// <summary>
    /// 执行 NormalizeDatabaseMode 方法。
    /// </summary>
    private static string NormalizeDatabaseMode(string databaseMode) {
        // 步骤：执行 NormalizeDatabaseMode 方法的核心处理流程。
        var mode = string.IsNullOrWhiteSpace(databaseMode) ? "Auto" : databaseMode.Trim();
        if (mode.Equals("Auto", StringComparison.OrdinalIgnoreCase)) {
            return "Auto";
        }

        if (mode.Equals("ServiceName", StringComparison.OrdinalIgnoreCase)) {
            return "ServiceName";
        }

        if (mode.Equals("Sid", StringComparison.OrdinalIgnoreCase)) {
            return "Sid";
        }

        throw new InvalidOperationException("Oracle.DatabaseMode 仅支持 Auto、ServiceName、Sid。");
    }
}

