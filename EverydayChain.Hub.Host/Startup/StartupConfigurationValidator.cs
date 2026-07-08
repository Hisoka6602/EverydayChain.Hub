using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Configuration;

namespace EverydayChain.Hub.Host.Startup;

/// <summary>
/// 校验宿主启动所需的关键配置。
/// </summary>
public static class StartupConfigurationValidator
{
    /// <summary>
    /// 存储连接字符串占位标记。
    /// </summary>
    private const string PlaceholderToken = "__SET_VIA_";

    /// <summary>
    /// 校验关键配置是否已经替换为真实值。
    /// </summary>
    /// <param name="configuration">应用配置。</param>
    public static void Validate(IConfiguration configuration)
    {
        var shardingOptions = configuration.GetSection(ShardingOptions.SectionName).Get<ShardingOptions>() ?? new ShardingOptions();
        EnsureConnectionStringConfigured(shardingOptions.ConnectionString, $"{ShardingOptions.SectionName}.ConnectionString");

        if (!IsOracleConnectionRequired(configuration))
        {
            return;
        }

        var oracleOptions = configuration.GetSection(OracleOptions.SectionName).Get<OracleOptions>() ?? new OracleOptions();
        EnsureConnectionStringConfigured(oracleOptions.ConnectionString, $"{OracleOptions.SectionName}.ConnectionString");
    }

    /// <summary>
    /// 判断当前部署是否要求提供 Oracle 连接。
    /// </summary>
    /// <param name="configuration">应用配置。</param>
    /// <returns>需要 Oracle 连接时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private static bool IsOracleConnectionRequired(IConfiguration configuration)
    {
        var syncJobOptions = configuration.GetSection(SyncJobOptions.SectionName).Get<SyncJobOptions>() ?? new SyncJobOptions();
        if ((syncJobOptions.Tables ?? []).Any(table => table.Enabled))
        {
            return true;
        }

        var wmsFeedbackOptions = configuration.GetSection(WmsFeedbackOptions.SectionName).Get<WmsFeedbackOptions>() ?? new WmsFeedbackOptions();
        if (wmsFeedbackOptions.Enabled)
        {
            return true;
        }

        var compensationOptions = configuration.GetSection(FeedbackCompensationJobOptions.SectionName).Get<FeedbackCompensationJobOptions>() ?? new FeedbackCompensationJobOptions();
        return compensationOptions.Enabled;
    }

    /// <summary>
    /// 校验连接字符串是否已经配置真实值。
    /// </summary>
    /// <param name="connectionString">连接字符串。</param>
    /// <param name="configurationPath">配置路径。</param>
    private static void EnsureConnectionStringConfigured(string? connectionString, string configurationPath)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{configurationPath} 不能为空。请通过环境变量、User Secrets 或部署配置提供真实连接字符串。");
        }

        if (connectionString.Contains(PlaceholderToken, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{configurationPath} 仍是占位值。请通过环境变量、User Secrets 或部署配置覆盖默认占位字符串。");
        }
    }
}
