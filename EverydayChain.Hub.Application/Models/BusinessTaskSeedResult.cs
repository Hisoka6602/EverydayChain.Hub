namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 业务任务模拟补数执行结果。
/// </summary>
public sealed class BusinessTaskSeedResult
{
    /// <summary>
    /// 执行是否成功。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 结果消息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 目标物理表名。
    /// </summary>
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 请求条码总数。
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// 过滤空白条码数量。
    /// </summary>
    public int FilteredEmptyCount { get; set; }

    /// <summary>
    /// 请求内重复条码数量。
    /// </summary>
    public int DeduplicatedCount { get; set; }

    /// <summary>
    /// 清洗去重后条码数量。
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// 成功插入数量。
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>
    /// 目标表内已存在而跳过数量。
    /// </summary>
    public int SkippedExistingCount { get; set; }

    /// <summary>
    /// 构建失败结果。
    /// </summary>
    /// <param name="message">失败消息。</param>
    /// <returns>失败结果。</returns>
    public static BusinessTaskSeedResult Fail(string message)
    {
        return new BusinessTaskSeedResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}
