namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义 ISqlExecutionTuner 类型。
/// </summary>
public interface ISqlExecutionTuner
{
    /// <summary>
    /// 获取或设置 CurrentBatchSize。
    /// </summary>
    int CurrentBatchSize { get; }

    /// <summary>
    /// 执行 Record 方法。
    /// </summary>
    void Record(TimeSpan elapsed, bool success);
}


