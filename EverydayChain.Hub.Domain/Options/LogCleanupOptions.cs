namespace EverydayChain.Hub.Domain.Options;

/// <summary>
/// 定义 LogCleanupOptions 类型。
/// </summary>
public sealed class LogCleanupOptions
{
    /// <summary>
    /// 存储 SectionName 字段。
    /// </summary>
    public const string SectionName = "LogCleanup";

    /// <summary>
    /// 获取或设置 Enabled。
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 获取或设置 RetentionDays。
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// 获取或设置 CheckIntervalHours。
    /// </summary>
    public int CheckIntervalHours { get; set; } = 6;

    /// <summary>
    /// 获取或设置 LogDirectory。
    /// </summary>
    public string LogDirectory { get; set; } = "logs";

    /// <summary>
    /// 获取或设置 LowDiskFreeSpaceMbWarningThreshold。
    /// </summary>
    public long LowDiskFreeSpaceMbWarningThreshold { get; set; } = 1024;

    /// <summary>
    /// 获取或设置 LowDiskFreeSpacePercentWarningThreshold。
    /// </summary>
    public int LowDiskFreeSpacePercentWarningThreshold { get; set; } = 10;

    /// <summary>
    /// 获取或设置 LogDirectorySizeMbWarningThreshold。
    /// </summary>
    public long LogDirectorySizeMbWarningThreshold { get; set; } = 1024;

    /// <summary>
    /// 获取或设置 StartupMinimumFreeSpaceMb。
    /// </summary>
    public long StartupMinimumFreeSpaceMb { get; set; } = 200;

    /// <summary>
    /// 校验配置是否合法。
    /// </summary>
    /// <returns>错误消息列表。</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (RetentionDays <= 0)
        {
            errors.Add("LogCleanup.RetentionDays 必须大于 0。");
        }

        if (CheckIntervalHours <= 0)
        {
            errors.Add("LogCleanup.CheckIntervalHours 必须大于 0。");
        }

        if (string.IsNullOrWhiteSpace(LogDirectory))
        {
            errors.Add("LogCleanup.LogDirectory 不能为空。");
        }

        if (LowDiskFreeSpaceMbWarningThreshold < 0)
        {
            errors.Add("LogCleanup.LowDiskFreeSpaceMbWarningThreshold 不能小于 0。");
        }

        if (LowDiskFreeSpacePercentWarningThreshold is < 0 or > 100)
        {
            errors.Add("LogCleanup.LowDiskFreeSpacePercentWarningThreshold 必须在 0 到 100 之间。");
        }

        if (LogDirectorySizeMbWarningThreshold < 0)
        {
            errors.Add("LogCleanup.LogDirectorySizeMbWarningThreshold 不能小于 0。");
        }

        if (StartupMinimumFreeSpaceMb < 0)
        {
            errors.Add("LogCleanup.StartupMinimumFreeSpaceMb 不能小于 0。");
        }

        return errors;
    }
}
