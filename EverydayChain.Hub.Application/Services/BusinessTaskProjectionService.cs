using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 定义 BusinessTaskProjectionService 类型。
/// </summary>
public class BusinessTaskProjectionService : IBusinessTaskProjectionService
{
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

    private static BusinessTaskEntity BuildEntity(BusinessTaskProjectionRow row)
    {
        var sourceTableCode = ValidateRequiredText(row.SourceTableCode, nameof(row.SourceTableCode), 64);
        var businessKey = ValidateRequiredText(row.BusinessKey, nameof(row.BusinessKey), 64);
        if (row.SourceType == BusinessTaskSourceType.Unknown)
        {
            throw new ArgumentException("SourceType 不可为 Unknown。", nameof(row.SourceType));
        }

        var barcode = ValidateOptionalText(row.Barcode, nameof(row.Barcode), 128);
        var waveCode = ValidateOptionalText(row.WaveCode, nameof(row.WaveCode), 64);
        var waveRemark = ValidateOptionalText(row.WaveRemark, nameof(row.WaveRemark), 128);
        var workingArea = ValidateOptionalText(row.WorkingArea, nameof(row.WorkingArea), 32);
        var orderId = ValidateOptionalText(row.OrderId, nameof(row.OrderId), 64);
        var storeId = ValidateOptionalText(row.StoreId, nameof(row.StoreId), 64);
        var storeName = ValidateOptionalText(row.StoreName, nameof(row.StoreName), 300);
        var productCode = ValidateOptionalText(row.ProductCode, nameof(row.ProductCode), 64);
        var pickLocation = ValidateOptionalText(row.PickLocation, nameof(row.PickLocation), 64);
        var projectedTimeLocal = ValidateProjectedTimeLocal(row);
        var updatedTimeLocal = DateTime.Now;

        var entity = new BusinessTaskEntity
        {
            TaskCode = businessKey,
            SourceTableCode = sourceTableCode,
            SourceType = row.SourceType,
            BusinessKey = businessKey,
            Barcode = barcode,
            WaveCode = waveCode,
            WaveRemark = waveRemark,
            WorkingArea = workingArea,
            OrderId = orderId,
            StoreId = storeId,
            StoreName = storeName,
            ProductCode = productCode,
            PickLocation = pickLocation,
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = projectedTimeLocal,
            UpdatedTimeLocal = updatedTimeLocal
        };
        entity.RefreshQueryFields();
        return entity;
    }

    private static DateTime ValidateProjectedTimeLocal(BusinessTaskProjectionRow row)
    {
        if (row.ProjectedTimeLocal == default)
        {
            throw new InvalidOperationException(
                $"ProjectedTimeLocal 缺失，无法确定分表月份，禁止继续写入业务任务。SourceTableCode={row.SourceTableCode}, BusinessKey={row.BusinessKey}");
        }

        return row.ProjectedTimeLocal;
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

