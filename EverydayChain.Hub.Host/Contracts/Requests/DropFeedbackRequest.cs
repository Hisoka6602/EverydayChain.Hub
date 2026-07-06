using System.ComponentModel.DataAnnotations;

namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class DropFeedbackRequest {
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(64)]
    public string? TaskCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string ActualChuteCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime DropTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    [MaxLength(256)]
    public string? FailureReason { get; set; }
}

