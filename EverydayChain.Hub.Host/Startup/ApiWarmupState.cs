namespace EverydayChain.Hub.Host.Startup;

/// <summary>
/// 表示启动预热状态快照。
/// </summary>
/// <param name="HasStarted">是否已开始预热。</param>
/// <param name="IsRunning">是否仍在预热中。</param>
/// <param name="IsCompleted">是否已完成预热。</param>
/// <param name="Stage">当前预热阶段。</param>
/// <param name="Message">当前预热说明。</param>
/// <param name="StartedAtLocal">预热开始时间。</param>
/// <param name="CompletedAtLocal">预热完成时间。</param>
public sealed record ApiWarmupStateSnapshot(
    bool HasStarted,
    bool IsRunning,
    bool IsCompleted,
    string Stage,
    string Message,
    DateTime? StartedAtLocal,
    DateTime? CompletedAtLocal);

/// <summary>
/// 定义启动预热状态服务。
/// </summary>
public interface IApiWarmupState
{
    /// <summary>
    /// 获取当前预热状态快照。
    /// </summary>
    /// <returns>预热状态快照。</returns>
    ApiWarmupStateSnapshot GetSnapshot();

    /// <summary>
    /// 标记预热已开始。
    /// </summary>
    /// <param name="stage">当前阶段。</param>
    /// <param name="message">阶段说明。</param>
    void MarkStarted(string stage, string message);

    /// <summary>
    /// 更新预热进度。
    /// </summary>
    /// <param name="stage">当前阶段。</param>
    /// <param name="message">阶段说明。</param>
    void MarkProgress(string stage, string message);

    /// <summary>
    /// 标记预热已完成。
    /// </summary>
    /// <param name="stage">完成阶段。</param>
    /// <param name="message">完成说明。</param>
    void MarkCompleted(string stage, string message);

    /// <summary>
    /// 标记预热已跳过。
    /// </summary>
    /// <param name="stage">跳过阶段。</param>
    /// <param name="message">跳过说明。</param>
    void MarkSkipped(string stage, string message);

    /// <summary>
    /// 标记预热失败。
    /// </summary>
    /// <param name="stage">失败阶段。</param>
    /// <param name="message">失败说明。</param>
    void MarkFailed(string stage, string message);
}

/// <summary>
/// 提供启动预热状态的线程安全存储。
/// </summary>
public sealed class ApiWarmupState : IApiWarmupState
{
    private readonly object _syncRoot = new();
    private ApiWarmupStateSnapshot _snapshot = new(
        false,
        false,
        false,
        "Pending",
        "启动预热尚未开始。",
        null,
        null);

    /// <summary>
    /// 获取当前预热状态快照。
    /// </summary>
    /// <returns>预热状态快照。</returns>
    public ApiWarmupStateSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return _snapshot;
        }
    }

    /// <summary>
    /// 标记预热已开始。
    /// </summary>
    /// <param name="stage">当前阶段。</param>
    /// <param name="message">阶段说明。</param>
    public void MarkStarted(string stage, string message)
    {
        lock (_syncRoot)
        {
            var startedAtLocal = _snapshot.StartedAtLocal ?? DateTime.Now;
            _snapshot = new ApiWarmupStateSnapshot(
                true,
                true,
                false,
                stage,
                message,
                startedAtLocal,
                null);
        }
    }

    /// <summary>
    /// 更新预热进度。
    /// </summary>
    /// <param name="stage">当前阶段。</param>
    /// <param name="message">阶段说明。</param>
    public void MarkProgress(string stage, string message)
    {
        lock (_syncRoot)
        {
            var startedAtLocal = _snapshot.StartedAtLocal ?? DateTime.Now;
            _snapshot = new ApiWarmupStateSnapshot(
                true,
                true,
                false,
                stage,
                message,
                startedAtLocal,
                null);
        }
    }

    /// <summary>
    /// 标记预热已完成。
    /// </summary>
    /// <param name="stage">完成阶段。</param>
    /// <param name="message">完成说明。</param>
    public void MarkCompleted(string stage, string message)
    {
        lock (_syncRoot)
        {
            var startedAtLocal = _snapshot.StartedAtLocal ?? DateTime.Now;
            _snapshot = new ApiWarmupStateSnapshot(
                true,
                false,
                true,
                stage,
                message,
                startedAtLocal,
                DateTime.Now);
        }
    }

    /// <summary>
    /// 标记预热已跳过。
    /// </summary>
    /// <param name="stage">跳过阶段。</param>
    /// <param name="message">跳过说明。</param>
    public void MarkSkipped(string stage, string message)
    {
        lock (_syncRoot)
        {
            var startedAtLocal = _snapshot.StartedAtLocal ?? DateTime.Now;
            _snapshot = new ApiWarmupStateSnapshot(
                true,
                false,
                false,
                stage,
                message,
                startedAtLocal,
                DateTime.Now);
        }
    }

    /// <summary>
    /// 标记预热失败。
    /// </summary>
    /// <param name="stage">失败阶段。</param>
    /// <param name="message">失败说明。</param>
    public void MarkFailed(string stage, string message)
    {
        lock (_syncRoot)
        {
            var startedAtLocal = _snapshot.StartedAtLocal ?? DateTime.Now;
            _snapshot = new ApiWarmupStateSnapshot(
                true,
                false,
                false,
                stage,
                message,
                startedAtLocal,
                DateTime.Now);
        }
    }
}
