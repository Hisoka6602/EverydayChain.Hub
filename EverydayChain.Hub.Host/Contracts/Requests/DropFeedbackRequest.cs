using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 表示落格回传请求参数。
/// </summary>
public sealed class DropFeedbackRequest {
    /// <summary>
    /// 表示本次落格回传关联的业务任务编码。
    /// </summary>
    [MaxLength(64)]
    public string? TaskCode { get; set; }

    /// <summary>
    /// 表示本次落格回传对应的箱码或业务条码。
    /// </summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    /// <summary>
    /// 表示设备实际上落入的格口编码。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ActualChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 表示实际发生落格的本地时间。
    /// </summary>
    public DateTime DropTimeLocal { get; set; }

    /// <summary>
    /// 表示当前接口调用或回传结果是否处理成功。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 表示落格失败或异常时的原因说明。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }
}

