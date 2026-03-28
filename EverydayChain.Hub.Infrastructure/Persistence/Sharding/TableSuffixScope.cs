namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

public static class TableSuffixScope {
    private static readonly AsyncLocal<string?> Current = new();

    public static string? CurrentSuffix => Current.Value;

    public static IDisposable Use(string suffix) {
        var previous = Current.Value;
        Current.Value = suffix;
        return new Scope(() => Current.Value = previous);
    }

    private sealed class Scope(Action onDispose) : IDisposable {
        public void Dispose() => onDispose();
    }
}
