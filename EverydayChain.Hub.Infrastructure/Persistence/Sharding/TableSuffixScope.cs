namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 定义 TableSuffixScope 类型。
/// </summary>
public static class TableSuffixScope
{
    private static readonly AsyncLocal<string?> Current = new();

    /// <summary>
    /// 获取或设置 CurrentSuffix。
    /// </summary>
    public static string? CurrentSuffix => Current.Value;

    public static IDisposable Use(string suffix)
    {
        var previous = Current.Value;
        Current.Value = suffix;
        return new Scope(() => Current.Value = previous);
    }

    /// <summary>
    /// 定义 Scope 类型。
    /// </summary>
    private sealed class Scope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}

