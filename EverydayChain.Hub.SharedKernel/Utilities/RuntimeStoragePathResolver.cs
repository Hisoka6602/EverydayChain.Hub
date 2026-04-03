namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 运行期存储路径解析工具。
/// </summary>
public static class RuntimeStoragePathResolver
{
    /// <summary>
    /// 将配置路径解析为绝对路径。
    /// </summary>
    /// <param name="configuredPath">配置路径。</param>
    /// <param name="defaultRelativePath">默认相对路径。</param>
    /// <returns>绝对路径。</returns>
    public static string ResolveAbsolutePath(string configuredPath, string defaultRelativePath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(AppContext.BaseDirectory, defaultRelativePath);
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
    }
}
