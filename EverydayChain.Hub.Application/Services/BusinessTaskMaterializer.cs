using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义 BusinessTaskMaterializer 类型。
/// </summary>
public class BusinessTaskMaterializer : IBusinessTaskMaterializer
{
    public BusinessTaskEntity Materialize(BusinessTaskMaterializeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var taskCode = ValidateRequiredText(request.TaskCode, nameof(request.TaskCode), 64);
        var sourceTableCode = ValidateRequiredText(request.SourceTableCode, nameof(request.SourceTableCode), 64);
        var businessKey = ValidateRequiredText(request.BusinessKey, nameof(request.BusinessKey), 256);
        var barcode = ValidateOptionalText(request.Barcode, nameof(request.Barcode), 128);
        var waveCode = ValidateOptionalText(request.WaveCode, nameof(request.WaveCode), 64);
        var waveRemark = ValidateOptionalText(request.WaveRemark, nameof(request.WaveRemark), 128);
        var materializedTimeLocal = ValidateMaterializedTimeLocal(request.MaterializedTimeLocal, request.TaskCode, request.SourceTableCode);

        var entity = new BusinessTaskEntity
        {
            TaskCode = taskCode,
            SourceTableCode = sourceTableCode,
            SourceType = request.SourceType,
            BusinessKey = businessKey,
            Barcode = barcode,
            WaveCode = waveCode,
            WaveRemark = waveRemark,
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = materializedTimeLocal,
            UpdatedTimeLocal = materializedTimeLocal,
        };
        entity.RefreshQueryFields();
        return entity;
    }

    private static DateTime ValidateMaterializedTimeLocal(DateTime materializedTimeLocal, string taskCode, string sourceTableCode)
    {
        if (materializedTimeLocal == default)
        {
            throw new InvalidOperationException(
                $"MaterializedTimeLocal 缺失，无法确定业务时间，禁止继续写入业务任务。TaskCode={taskCode}, SourceTableCode={sourceTableCode}");
        }

        return materializedTimeLocal;
    }

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

