namespace EverydayChain.Hub.Infrastructure.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface ISqlExecutionTuner
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    int CurrentBatchSize { get; }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    void Record(TimeSpan elapsed, bool success);
}


