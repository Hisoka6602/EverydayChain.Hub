namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 表示回流汇总中的单行统计结果。
/// </summary>
public sealed class RecirculationSummaryRowResponse
{
    /// <summary>
    /// 表示落格或统计对应的格口编码。
    /// </summary>
    public string Chute { get; set; } = string.Empty;

    /// <summary>
    /// 表示波次号。
    /// </summary>
    public string WaveNo { get; set; } = string.Empty;

    /// <summary>
    /// 表示回流次数。
    /// </summary>
    public int Reflow { get; set; }
}

