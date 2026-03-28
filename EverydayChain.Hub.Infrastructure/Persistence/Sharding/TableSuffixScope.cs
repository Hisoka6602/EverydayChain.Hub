namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 基于 <see cref="AsyncLocal{T}"/> 的分表后缀作用域，保证异步上下文中路由隔离。
/// </summary>
public static class TableSuffixScope
{
    /// <summary>当前异步上下文的分表后缀存储。</summary>
    private static readonly AsyncLocal<string?> Current = new();

    /// <summary>
    /// 获取当前异步上下文的分表后缀；若未设置则返回 <c>null</c>。
    /// </summary>
    public static string? CurrentSuffix => Current.Value;

    /// <summary>
    /// 设置当前异步上下文的分表后缀，并返回离开作用域时自动还原的 <see cref="IDisposable"/>。
    /// </summary>
    /// <param name="suffix">要激活的分表后缀，例如 <c>_202603</c>。</param>
    /// <returns>用于还原前一后缀的作用域对象。</returns>
    public static IDisposable Use(string suffix)
    {
        var previous = Current.Value;
        Current.Value = suffix;
        return new Scope(() => Current.Value = previous);
    }

    /// <summary>
    /// 轻量级作用域对象，析构时执行回调还原后缀。
    /// </summary>
    private sealed class Scope(Action onDispose) : IDisposable
    {
        /// <summary>还原分表后缀到进入作用域前的值。</summary>
        public void Dispose() => onDispose();
    }
}
