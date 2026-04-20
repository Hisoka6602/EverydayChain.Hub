using EverydayChain.Hub.Domain.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

/// <summary>
/// 本地统一业务任务实体，承载扫描、格口与落格链路的主状态。
/// </summary>
public class BusinessTaskEntity : IEntity<long>
{
    /// <summary>
    /// 主键标识。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 业务任务编码（来自上游任务主键或业务号），最大 64 字符。
    /// </summary>
    [MaxLength(64)]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 来源同步表编码，最大 64 字符。
    /// </summary>
    [MaxLength(64)]
    public string SourceTableCode { get; set; } = string.Empty;

    /// <summary>
    /// 来源类型，区分拆零与整件链路。
    /// </summary>
    public BusinessTaskSourceType SourceType { get; set; } = BusinessTaskSourceType.Unknown;

    /// <summary>
    /// 业务键文本（由唯一键拼接得到），最大 256 字符。
    /// </summary>
    [MaxLength(256)]
    public string BusinessKey { get; set; } = string.Empty;

    /// <summary>
    /// 条码文本，最大 128 字符；尚未关联条码时可为空。
    /// </summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    /// <summary>
    /// 条码标准化字段（去除前后空白后写入），最大 128 字符；用于高频查询等值匹配。
    /// 空值语义：当条码为空白时写入空值。
    /// </summary>
    [MaxLength(128)]
    public string? NormalizedBarcode { get; set; }

    /// <summary>
    /// 目标格口编码，由格口规则计算得出，最大 64 字符；未分配格口时可为空。
    /// </summary>
    [MaxLength(64)]
    public string? TargetChuteCode { get; set; }

    /// <summary>
    /// 实际落格编码，落格回传时写入，最大 64 字符；尚未落格时可为空。
    /// </summary>
    [MaxLength(64)]
    public string? ActualChuteCode { get; set; }

    /// <summary>
    /// 查询专用归并码头编码，优先取实际落格编码，其次取目标格口编码，均为空时写入占位值。
    /// 可填写范围：长度 1~64 的文本；默认占位值为“未分配码头”。
    /// </summary>
    [MaxLength(64)]
    public string ResolvedDockCode { get; set; } = "未分配码头";

    /// <summary>
    /// 扫描设备编码，最大 64 字符；尚未扫描时可为空。
    /// </summary>
    [MaxLength(64)]
    public string? DeviceCode { get; set; }

    /// <summary>
    /// 链路追踪标识，最大 64 字符；可为空。
    /// </summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>
    /// 失败原因文本，最大 256 字符；正常状态下为空。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// 任务当前状态。
    /// </summary>
    public BusinessTaskStatus Status { get; set; } = BusinessTaskStatus.Created;

    /// <summary>
    /// 业务回传状态，标识回传进度。
    /// </summary>
    public BusinessTaskFeedbackStatus FeedbackStatus { get; set; } = BusinessTaskFeedbackStatus.NotRequired;

    /// <summary>
    /// 扫描时间（本地时间）；尚未扫描时为空。
    /// </summary>
    public DateTime? ScannedAtLocal { get; set; }

    /// <summary>
    /// 包裹长度，单位毫米；可为空。
    /// </summary>
    public decimal? LengthMm { get; set; }

    /// <summary>
    /// 包裹宽度，单位毫米；可为空。
    /// </summary>
    public decimal? WidthMm { get; set; }

    /// <summary>
    /// 包裹高度，单位毫米；可为空。
    /// </summary>
    public decimal? HeightMm { get; set; }

    /// <summary>
    /// 包裹体积，单位立方毫米；可为空。
    /// </summary>
    public decimal? VolumeMm3 { get; set; }

    /// <summary>
    /// 包裹重量，单位克；可为空。
    /// </summary>
    public decimal? WeightGram { get; set; }

    /// <summary>
    /// 扫描次数；每次扫描成功后递增。
    /// </summary>
    public int ScanCount { get; set; }

    /// <summary>
    /// 落格时间（本地时间）；尚未落格时为空。
    /// </summary>
    public DateTime? DroppedAtLocal { get; set; }

    /// <summary>
    /// 创建时间（本地时间）。
    /// </summary>
    public DateTime CreatedTimeLocal { get; set; }

    /// <summary>
    /// 更新时间（本地时间）。
    /// </summary>
    public DateTime UpdatedTimeLocal { get; set; }

    /// <summary>
    /// 波次编码，来自上游批次/波次标识；用于波次清理规则定位该批次所有任务，最大 64 字符；不参与波次管理时为空。
    /// </summary>
    [MaxLength(64)]
    public string? WaveCode { get; set; }

    /// <summary>
    /// 波次标准化字段（去除前后空白后写入），最大 64 字符；用于高频查询等值匹配。
    /// 空值语义：当波次为空白时写入空值。
    /// </summary>
    [MaxLength(64)]
    public string? NormalizedWaveCode { get; set; }

    /// <summary>
    /// 波次备注，来自上游波次说明信息，最大 128 字符；无备注时为空。
    /// </summary>
    [MaxLength(128)]
    public string? WaveRemark { get; set; }

    /// <summary>
    /// 工作区域编码，来自上游 WORKINGAREA 字段；用于拆零分区统计映射。
    /// </summary>
    [MaxLength(32)]
    public string? WorkingArea { get; set; }

    /// <summary>
    /// 是否已被标记为回流；由回流规则服务在扫描重试超限时置为 true。
    /// </summary>
    public bool IsRecirculated { get; set; }

    /// <summary>
    /// 是否处于异常状态；用于显式标识异常任务。
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// 扫描重试次数；每次扫描上传失败时由任务执行服务递增，用于触发回流判定。
    /// </summary>
    public int ScanRetryCount { get; set; }

    /// <summary>
    /// 回传标记；回传成功后置为 true。
    /// </summary>
    public bool IsFeedbackReported { get; set; }

    /// <summary>
    /// 回传时间（本地时间）；回传成功后写入。
    /// </summary>
    public DateTime? FeedbackTimeLocal { get; set; }

    /// <summary>
    /// 刷新查询优化字段，统一标准化条码、波次与归并码头编码。
    /// 当条码、波次、目标格口、实际落格任一字段发生变化后，应在持久化前调用该方法。
    /// </summary>
    public void RefreshQueryFields()
    {
        NormalizedBarcode = NormalizeOptionalText(Barcode);
        NormalizedWaveCode = NormalizeOptionalText(WaveCode);
        ResolvedDockCode = ResolveDockCode(ActualChuteCode, TargetChuteCode);
    }

    /// <summary>
    /// 归一化可选文本。
    /// </summary>
    /// <param name="value">原始文本。</param>
    /// <returns>归一化后的文本。</returns>
    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 解析归并码头编码。
    /// </summary>
    /// <param name="actualChuteCode">实际落格编码。</param>
    /// <param name="targetChuteCode">目标格口编码。</param>
    /// <returns>归并后的码头编码。</returns>
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
