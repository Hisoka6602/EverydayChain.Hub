using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 PassThroughSqlExecutionTuner 类型。
/// </summary>
public sealed class PassThroughSqlExecutionTuner : ISqlExecutionTuner
{
    /// <summary>
    /// 获取或设置 CurrentBatchSize。
    /// </summary>
    public int CurrentBatchSize => 100;

    public void Record(TimeSpan elapsed, bool success)
    {
    }
}

