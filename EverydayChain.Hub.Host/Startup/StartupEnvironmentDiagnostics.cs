namespace EverydayChain.Hub.Host.Startup;

/// <summary>
/// 提供启动环境相关的诊断提示。
/// </summary>
public static class StartupEnvironmentDiagnostics
{
    /// <summary>
    /// 只读联调环境名称。
    /// </summary>
    private const string ReadOnlySyncEnvironmentName = "ReadOnlySync";

    /// <summary>
    /// 根据当前环境与只读联调配置文件存在性，返回需要提示的启动告警。
    /// </summary>
    /// <param name="environmentName">当前环境名。</param>
    /// <param name="readOnlySyncConfigFileExists">是否存在只读联调配置文件。</param>
    /// <returns>启动阶段需要记录的告警消息集合。</returns>
    public static IReadOnlyList<string> GetWarnings(string? environmentName, bool readOnlySyncConfigFileExists)
    {
        var warnings = new List<string>();
        var isReadOnlySyncEnvironment = string.Equals(
            environmentName?.Trim(),
            ReadOnlySyncEnvironmentName,
            StringComparison.OrdinalIgnoreCase);
        if (readOnlySyncConfigFileExists && !isReadOnlySyncEnvironment)
        {
            warnings.Add("检测到 appsettings.ReadOnlySync.json 存在，但当前未启用 ReadOnlySync 环境；本次启动不会加载只读联调配置，将继续按当前环境配置运行。");
        }

        if (!readOnlySyncConfigFileExists && isReadOnlySyncEnvironment)
        {
            warnings.Add("当前环境已指定为 ReadOnlySync，但未找到 appsettings.ReadOnlySync.json；本次启动将回退到基础配置，请确认只读联调配置文件是否存在。");
        }

        return warnings;
    }
}
