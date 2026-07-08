using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

/// <summary>
/// 定义 BusinessTaskEntity 类型。
/// </summary>
public class BusinessTaskEntity : IEntity<long>
{
    /// <summary>
    /// 获取或设置 Id。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 获取或设置 TaskCode。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceTableCode。
    /// </summary>
    [MaxLength(64)]
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceType。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

    /// <summary>
    /// 获取或设置 BusinessKey。
    /// </summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Barcode。
    /// </summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置 NormalizedBarcode。
    /// </summary>
    [MaxLength(128)]
    public string? NormalizedBarcode { get; set; }

    /// <summary>
    /// 获取或设置 TargetChuteCode。
    /// </summary>
    [MaxLength(64)]
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 ActualChuteCode。
    /// </summary>
    [MaxLength(64)]
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 获取或设置 ResolvedDockCode。
    /// </summary>
    [MaxLength(64)]
    public string ResolvedDockCode { get; set; } = "未分配码头";

    /// <summary>
    /// 获取或设置 DeviceCode。
    /// </summary>
    [MaxLength(64)]
    public string? DeviceCode { get; set; }

    /// <summary>
    /// 获取或设置 TraceId。
    /// </summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 获取或设置 Status。
    /// </summary>
    public BusinessTaskStatus Status { get; set; } = BusinessTaskStatus.Created;

    /// <summary>
    /// 获取或设置 FeedbackStatus。
    /// </summary>
    public BusinessTaskFeedbackStatus FeedbackStatus { get; set; } = BusinessTaskFeedbackStatus.NotRequired;

    /// <summary>
    /// 获取或设置 ScannedAtLocal。
    /// </summary>
    public DateTime? ScannedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置 LengthMm。
    /// </summary>
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 获取或设置 WidthMm。
    /// </summary>
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 获取或设置 HeightMm。
    /// </summary>
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 获取或设置 VolumeMm3。
    /// </summary>
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 获取或设置 WeightGram。
    /// </summary>
    public decimal? WeightGram { get; set; }

    /// <summary>
    /// 获取或设置 ScanCount。
    /// </summary>
    public int ScanCount { get; set; }

    /// <summary>
    /// 获取或设置 DroppedAtLocal。
    /// </summary>
    public DateTime? DroppedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置 CreatedTimeLocal。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 UpdatedTimeLocal。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置 WaveCode。
    /// </summary>
    [MaxLength(64)]
    public string? WaveCode { get; set; }

    /// <summary>
    /// 获取或设置 NormalizedWaveCode。
    /// </summary>
    [MaxLength(64)]
    public string? NormalizedWaveCode { get; set; }

    /// <summary>
    /// 获取或设置 WaveRemark。
    /// </summary>
    [MaxLength(128)]
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 获取或设置 WorkingArea。
    /// </summary>
    [MaxLength(32)]
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 获取或设置 OrderId。
    /// </summary>
    [MaxLength(64)]
    public string? OrderId { get; set; }

    /// <summary>
    /// 获取或设置 StoreId。
    /// </summary>
    [MaxLength(64)]
    public string? StoreId { get; set; }

    /// <summary>
    /// 获取或设置 StoreName。
    /// </summary>
    [MaxLength(300)]
    public string? StoreName { get; set; }

    /// <summary>
    /// 获取或设置 ProductCode。
    /// </summary>
    [MaxLength(64)]
    public string? ProductCode { get; set; }

    /// <summary>
    /// 获取或设置 PickLocation。
    /// </summary>
    [MaxLength(64)]
    public string? PickLocation { get; set; }

    /// <summary>
    /// 获取或设置 IsRecirculated。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 获取或设置 IsException。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 获取或设置 ScanRetryCount。
    /// </summary>
    public int ScanRetryCount { get; set; }

    /// <summary>
    /// 获取或设置 IsFeedbackReported。
    /// </summary>
    public bool IsFeedbackReported { get; set; }

    /// <summary>
    /// 获取或设置 FeedbackTimeLocal。
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

