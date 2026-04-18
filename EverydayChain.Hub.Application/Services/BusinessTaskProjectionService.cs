using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 业务任务投影服务实现，仅负责投影字段校验、标准化与实体构建。
/// </summary>
public class BusinessTaskProjectionService : IBusinessTaskProjectionService
{
    /// <inheritdoc />
    public BusinessTaskProjectionResult Project(BusinessTaskProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Rows.Count == 0)
        {
            return new BusinessTaskProjectionResult();
        }

        var entities = new List<BusinessTaskEntity>(request.Rows.Count);
        foreach (var row in request.Rows)
        {
            ArgumentNullException.ThrowIfNull(row);
            entities.Add(BuildEntity(row));
        }

        return new BusinessTaskProjectionResult
        {
            Entities = entities
        };
    }

    /// <summary>
    /// 根据投影行构建业务任务实体。
    /// </summary>
    /// <param name="row">投影行。</param>
    /// <returns>业务任务实体。</returns>
    private static BusinessTaskEntity BuildEntity(BusinessTaskProjectionRow row)
    {
        var sourceTableCode = ValidateRequiredText(row.SourceTableCode, nameof(row.SourceTableCode), 64);
        var businessKey = ValidateRequiredText(row.BusinessKey, nameof(row.BusinessKey), 256);
        if (row.SourceType == BusinessTaskSourceType.Unknown)
        {
            throw new ArgumentException("SourceType 不可为 Unknown。", nameof(row.SourceType));
        }

        var barcode = ValidateOptionalText(row.Barcode, nameof(row.Barcode), 128);
        var waveCode = ValidateOptionalText(row.WaveCode, nameof(row.WaveCode), 64);
        var waveRemark = ValidateOptionalText(row.WaveRemark, nameof(row.WaveRemark), 128);
        var projectedTimeLocal = row.ProjectedTimeLocal == default ? DateTime.Now : row.ProjectedTimeLocal;

        var entity = new BusinessTaskEntity
        {
            TaskCode = businessKey,
            SourceTableCode = sourceTableCode,
            SourceType = row.SourceType,
            BusinessKey = businessKey,
            Barcode = barcode,
            WaveCode = waveCode,
            WaveRemark = waveRemark,
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = projectedTimeLocal,
            UpdatedTimeLocal = projectedTimeLocal
        };
        entity.RefreshQueryFields();
        return entity;
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
