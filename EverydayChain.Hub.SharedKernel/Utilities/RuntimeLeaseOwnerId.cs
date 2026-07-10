using System.Globalization;

namespace EverydayChain.Hub.SharedKernel.Utilities;

/// <summary>
/// 提供运行时租约持有者标识的生成与解析能力。
/// </summary>
public static class RuntimeLeaseOwnerId
{
    /// <summary>
    /// 存储租约持有者标识允许的最大长度。
    /// </summary>
    private const int MaxOwnerIdLength = 64;

    /// <summary>
    /// 存储随机令牌固定长度。
    /// </summary>
    private const int TokenLength = 32;

    /// <summary>
    /// 生成可用于运行时租约的持有者标识。
    /// </summary>
    /// <returns>由机器名、进程号与随机令牌组成的持有者标识。</returns>
    public static string Create()
    {
        var processIdText = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
        var maxMachineNameLength = Math.Max(1, MaxOwnerIdLength - processIdText.Length - TokenLength - 2);
        var machineName = string.IsNullOrWhiteSpace(Environment.MachineName)
            ? "unknown"
            : Environment.MachineName.Trim();
        if (machineName.Length > maxMachineNameLength)
        {
            machineName = machineName[..maxMachineNameLength];
        }

        return $"{machineName}:{processIdText}:{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 解析运行时租约持有者标识。
    /// </summary>
    /// <param name="ownerId">待解析的持有者标识。</param>
    /// <param name="descriptor">解析成功后的结构化结果。</param>
    /// <returns>解析成功返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool TryParse(string? ownerId, out RuntimeLeaseOwnerDescriptor descriptor)
    {
        descriptor = default;
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return false;
        }

        var parts = ownerId.Split(':', 3, StringSplitOptions.TrimEntries);
        if (parts.Length != 3
            || string.IsNullOrWhiteSpace(parts[0])
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var processId)
            || processId <= 0
            || string.IsNullOrWhiteSpace(parts[2]))
        {
            return false;
        }

        descriptor = new RuntimeLeaseOwnerDescriptor(parts[0], processId, parts[2]);
        return true;
    }

    /// <summary>
    /// 表示租约持有者标识的结构化信息。
    /// </summary>
    /// <param name="MachineName">机器名。</param>
    /// <param name="ProcessId">进程号。</param>
    /// <param name="Token">随机令牌。</param>
    public readonly record struct RuntimeLeaseOwnerDescriptor(string MachineName, int ProcessId, string Token);
}
