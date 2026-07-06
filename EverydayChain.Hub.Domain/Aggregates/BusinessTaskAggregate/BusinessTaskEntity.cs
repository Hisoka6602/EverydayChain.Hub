using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

/// <summary>
/// 定义当前类型。
/// </summary>
public class BusinessTaskEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(128)]
    public string? NormalizedBarcode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string ResolvedDockCode { get; set; } = "未分配码头";

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? DeviceCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskStatus Status { get; set; } = BusinessTaskStatus.Created;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskFeedbackStatus FeedbackStatus { get; set; } = BusinessTaskFeedbackStatus.NotRequired;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? ScannedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public decimal? WeightGram { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ScanCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? DroppedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? WaveCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? NormalizedWaveCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(128)]
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(32)]
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? OrderId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? StoreId { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(300)]
    public string? StoreName { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? ProductCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? PickLocation { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ScanRetryCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsFeedbackReported { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? FeedbackTimeLocal { get; set; }

    public void RefreshQueryFields()
    {
        NormalizedBarcode = NormalizeOptionalText(Barcode);
        NormalizedWaveCode = NormalizeOptionalText(WaveCode);
        ResolvedDockCode = ResolveDockCode(ActualChuteCode, TargetChuteCode);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ResolveDockCode(string? actualChuteCode, string? targetChuteCode)
    {
        if (!string.IsNullOrWhiteSpace(actualChuteCode))
        {
            return actualChuteCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(targetChuteCode))
        {
            return targetChuteCode.Trim();
        }

        return "未分配码头";
    }
}

