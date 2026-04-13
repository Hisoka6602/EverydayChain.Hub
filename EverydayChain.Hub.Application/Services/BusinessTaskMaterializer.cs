using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 业务任务物化服务实现，仅负责字段映射、规范化与默认值填充。
/// </summary>
public class BusinessTaskMaterializer : IBusinessTaskMaterializer
{
    /// <inheritdoc />
    public BusinessTaskEntity Materialize(BusinessTaskMaterializeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var taskCode = ValidateRequiredText(request.TaskCode, nameof(request.TaskCode), 64);
        var sourceTableCode = ValidateRequiredText(request.SourceTableCode, nameof(request.SourceTableCode), 64);
        var businessKey = ValidateRequiredText(request.BusinessKey, nameof(request.BusinessKey), 256);
        var barcode = ValidateOptionalText(request.Barcode, nameof(request.Barcode), 128);
        var materializedTimeLocal = request.MaterializedTimeLocal ?? DateTime.Now;

        return new BusinessTaskEntity
        {
            TaskCode = taskCode,
            SourceTableCode = sourceTableCode,
            BusinessKey = businessKey,
            Barcode = barcode,
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = materializedTimeLocal,
            UpdatedTimeLocal = materializedTimeLocal,
        };
    }

    /// <summary>
    /// 校验必填文本并返回去空白后的值。
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <param name="fieldName">字段名称。</param>
    /// <param name="maxLength">最大长度。</param>
    /// <returns>去空白后的文本。</returns>
    private static string ValidateRequiredText(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} 不能为空白。", fieldName);
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} 长度不能超过 {maxLength}。", fieldName);
        }

        return normalizedValue;
    }

    /// <summary>
    /// 校验可选文本并返回去空白后的值。
    /// </summary>
    /// <param name="value">输入值。</param>
    /// <param name="fieldName">字段名称。</param>
    /// <param name="maxLength">最大长度。</param>
    /// <returns>去空白后的文本或空值。</returns>
    private static string? ValidateOptionalText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} 长度不能超过 {maxLength}。", fieldName);
        }

        return normalizedValue;
    }
}
