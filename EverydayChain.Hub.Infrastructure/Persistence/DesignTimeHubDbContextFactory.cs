using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Persistence.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EverydayChain.Hub.Infrastructure.Persistence;

/// <summary>
/// 提供 EF 设计时使用的 HubDbContext 工厂。
/// </summary>
public class DesignTimeHubDbContextFactory : IDesignTimeDbContextFactory<HubDbContext>
{
    /// <summary>
    /// 创建设计时 DbContext。
    /// </summary>
    /// <param name="args">设计时命令行参数。</param>
    /// <returns>HubDbContext 实例。</returns>
    public HubDbContext CreateDbContext(string[] args)
    {
        // 步骤：按命令行环境名加载配置，并允许设计时参数显式覆盖分片连接串。
        var hostProjectDirectory = ResolveHostProjectDirectory();
        var environmentName = ResolveEnvironmentName(args);
        var configuration = new ConfigurationBuilder()
            .SetBasePath(hostProjectDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<HubDbContext>();
        var shardingOptions = configuration.GetSection(ShardingOptions.SectionName).Get<ShardingOptions>() ?? new ShardingOptions();
        var commandLineConnectionString = ResolveShardingConnectionString(args);
        if (!string.IsNullOrWhiteSpace(commandLineConnectionString))
        {
            shardingOptions.ConnectionString = commandLineConnectionString.Trim();
        }

        optionsBuilder.UseSqlServer(shardingOptions.ConnectionString);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ShardModelCacheKeyFactory>();

        using var _ = TableSuffixScope.Use(string.Empty);
        return new HubDbContext(optionsBuilder.Options, Microsoft.Extensions.Options.Options.Create(shardingOptions));
    }

    /// <summary>
    /// 定位宿主项目目录。
    /// </summary>
    /// <returns>宿主项目目录绝对路径。</returns>
    private static string ResolveHostProjectDirectory()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var resolvedPath = TryFindHostProjectDirectory(startPath);
            if (!string.IsNullOrWhiteSpace(resolvedPath))
            {
                return resolvedPath;
            }
        }

        throw new InvalidOperationException("无法定位 EverydayChain.Hub.Host 目录，无法加载设计时配置。");
    }

    /// <summary>
    /// 递归向上查找宿主项目目录。
    /// </summary>
    /// <param name="startPath">起始搜索目录。</param>
    /// <returns>找到时返回宿主项目目录，否则返回 <c>null</c>。</returns>
    private static string? TryFindHostProjectDirectory(string startPath)
    {
        // 步骤：从当前目录逐级向上查找，兼容仓库根目录与宿主项目目录两种启动位置。
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var directHostProjectPath = Path.Combine(directory.FullName, "EverydayChain.Hub.Host");
            if (File.Exists(Path.Combine(directHostProjectPath, "appsettings.json")))
            {
                return directHostProjectPath;
            }

            if (File.Exists(Path.Combine(directory.FullName, "EverydayChain.Hub.Host.csproj"))
                && File.Exists(Path.Combine(directory.FullName, "appsettings.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// 解析设计时环境名。
    /// </summary>
    /// <param name="args">设计时命令行参数。</param>
    /// <returns>环境名。</returns>
    private static string ResolveEnvironmentName(string[] args)
    {
        var environmentName = TryGetCommandLineValue(args, "environment");
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return Environments.Production;
        }

        return environmentName.Trim();
    }

    /// <summary>
    /// 解析设计时命令行中的分片连接串。
    /// </summary>
    /// <param name="args">设计时命令行参数。</param>
    /// <returns>命中时返回连接串，否则返回 <c>null</c>。</returns>
    private static string? ResolveShardingConnectionString(string[] args)
    {
        // 步骤：兼容常见的设计时传参别名，避免依赖环境变量覆盖连接串。
        return TryGetCommandLineValue(args, "Sharding:ConnectionString")
            ?? TryGetCommandLineValue(args, "Sharding__ConnectionString")
            ?? TryGetCommandLineValue(args, "sharding-connection-string")
            ?? TryGetCommandLineValue(args, "connection-string");
    }

    /// <summary>
    /// 从命令行参数中读取指定键的值。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    /// <param name="key">目标键名。</param>
    /// <returns>命中时返回对应值，否则返回 <c>null</c>。</returns>
    private static string? TryGetCommandLineValue(string[] args, string key)
    {
        // 步骤：统一兼容 `--key value` 与 `--key=value` 两种传参格式。
        var normalizedKey = key.Trim().TrimStart('-');
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            var separatorIndex = argument.IndexOf('=');
            if (separatorIndex >= 0)
            {
                var candidateKey = argument[..separatorIndex].Trim().TrimStart('-');
                if (string.Equals(candidateKey, normalizedKey, StringComparison.OrdinalIgnoreCase))
                {
                    return argument[(separatorIndex + 1)..];
                }
            }

            var candidateName = argument.Trim().TrimStart('-');
            if (string.Equals(candidateName, normalizedKey, StringComparison.OrdinalIgnoreCase)
                && index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
