namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 定义当前类型。
/// </summary>
public static class RuntimeStoragePathResolver
{
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
