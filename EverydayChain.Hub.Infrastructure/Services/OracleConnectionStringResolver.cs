using Oracle.ManagedDataAccess.Client;
using EverydayChain.Hub.Domain.Options;

namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// Oracle 连接串解析器，统一处理 Database/DatabaseMode 对 Data Source 的覆写逻辑。
/// </summary>
internal static class OracleConnectionStringResolver {

    /// <summary>
    /// 构建生效连接字符串。
    /// </summary>
    /// <param name="options">Oracle 配置。</param>
    /// <returns>生效连接字符串。</returns>
    /// <exception cref="InvalidOperationException">当连接字符串为空或无法按库名重写 Data Source 时抛出。</exception>
    public static string BuildEffectiveConnectionString(OracleOptions options) {
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
    /// 以 EZCONNECT 形式重写 Data Source 中的库名。
    /// </summary>
    private static string OverrideOracleDatabase(string dataSource, string database, string databaseMode) {
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

        // 兜底策略：按 EZCONNECT ServiceName 形式拼接，产出 主机[:端口]/库名。
        return $"{trimmedDataSource}/{trimmedDatabase}";
    }

    /// <summary>
    /// 规范化库名模式文本。
    /// </summary>
    private static string NormalizeDatabaseMode(string databaseMode) {
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
