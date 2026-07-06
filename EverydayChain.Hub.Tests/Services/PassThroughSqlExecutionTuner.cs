using EverydayChain.Hub.Infrastructure.Services;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class PassThroughSqlExecutionTuner : ISqlExecutionTuner
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CurrentBatchSize => 100;

    public void Record(TimeSpan elapsed, bool success)
    {
    }
}

