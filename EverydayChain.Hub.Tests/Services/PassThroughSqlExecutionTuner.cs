using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 恒定批大小调谐器桩实现。
/// </summary>
public sealed class PassThroughSqlExecutionTuner : ISqlExecutionTuner
{
    /// <inheritdoc />
    public int CurrentBatchSize => 100;

    /// <inheritdoc />
    public void Record(TimeSpan elapsed, bool success)
    {
    }
}
