namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 批量写入性能自动调谐接口，根据执行结果动态调整批量写入窗口大小。
/// </summary>
public interface ISqlExecutionTuner
{
    /// <summary>
    /// 当前推荐的批量写入窗口大小。
    /// </summary>
    int CurrentBatchSize { get; }

    /// <summary>
    /// 记录一次批量写入的执行结果，用于调谐算法决策。
    /// </summary>
    /// <param name="elapsed">本次写入耗时。</param>
    /// <param name="success">本次写入是否成功。</param>
    void Record(TimeSpan elapsed, bool success);
}
