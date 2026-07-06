using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.MultiLabel.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.MultiLabel;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.MultiLabel.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class MultiLabelDecisionService : IMultiLabelDecisionService
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ExceptionRuleOptions _options;

    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly ILogger<MultiLabelDecisionService> _logger;

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    public MultiLabelDecisionService(
        IBusinessTaskRepository businessTaskRepository,
        ExceptionRuleOptions options,
        ILogger<MultiLabelDecisionService> logger)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        _businessTaskRepository = businessTaskRepository;
        _options = options;
        _logger = logger;
    }

    public async Task<MultiLabelDecisionResult> DecideAsync(string barcode, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.MultiLabel.Enabled)
        {
            _logger.LogDebug("多标签决策：规则开关关闭，跳过执行。Barcode={Barcode}", barcode);
            return new MultiLabelDecisionResult
            {
                IsMultiLabel = false,
                IsDecisionMade = true,
                Reason = "规则开关关闭。"
            };
        }

        if (string.IsNullOrWhiteSpace(barcode))
        {
            return new MultiLabelDecisionResult
            {
                IsMultiLabel = false,
                IsDecisionMade = false,
                Reason = "条码为空，无法决策。"
            };
        }

        var trimmedBarcode = barcode.Trim();

        var activeTasks = await _businessTaskRepository.FindActiveByBarcodeAsync(trimmedBarcode, ct);

        if (activeTasks.Count <= 1)
        {
            return new MultiLabelDecisionResult
            {
                IsMultiLabel = false,
                IsDecisionMade = true,
                SelectedTaskCode = activeTasks.Count == 1 ? activeTasks[0].TaskCode : null,
                Reason = "未检测到多标签场景。"
            };
        }

        _logger.LogWarning(
            "多标签决策：条码 {Barcode} 关联到 {Count} 个活跃任务，触发多标签决策。策略={Strategy}",
            trimmedBarcode, activeTasks.Count, _options.MultiLabel.Strategy);

        var allTaskCodes = activeTasks.Select(t => t.TaskCode).ToList();
        return _options.MultiLabel.Strategy switch
        {
            "UseFirst" => DecideByFirst(activeTasks, allTaskCodes),
            "UseLatest" => DecideByLatest(activeTasks, allTaskCodes),
            _ => DecideAsException(allTaskCodes, trimmedBarcode)
        };
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static MultiLabelDecisionResult DecideByFirst(
        IReadOnlyList<Domain.Aggregates.BusinessTaskAggregate.BusinessTaskEntity> tasks,
        List<string> allTaskCodes)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var selected = tasks.OrderBy(t => t.CreatedTimeLocal).First();
        var discarded = allTaskCodes.Where(c => c != selected.TaskCode).ToList();
        return new MultiLabelDecisionResult
        {
            IsMultiLabel = true,
            IsDecisionMade = true,
            SelectedTaskCode = selected.TaskCode,
            DiscardedTaskCodes = discarded,
            RecommendedStatus = BusinessTaskStatus.Exception,
            Reason = $"多标签策略 UseFirst：选用最早创建任务 {selected.TaskCode}，舍弃 {discarded.Count} 个任务。"
        };
    }

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    private static MultiLabelDecisionResult DecideByLatest(
        IReadOnlyList<Domain.Aggregates.BusinessTaskAggregate.BusinessTaskEntity> tasks,
        List<string> allTaskCodes)
    {
        // 步骤：按既定流程执行当前方法逻辑。
        var selected = tasks.OrderByDescending(t => t.CreatedTimeLocal).First();
        var discarded = allTaskCodes.Where(c => c != selected.TaskCode).ToList();
        return new MultiLabelDecisionResult
        {
            IsMultiLabel = true,
            IsDecisionMade = true,
            SelectedTaskCode = selected.TaskCode,
            DiscardedTaskCodes = discarded,
            RecommendedStatus = BusinessTaskStatus.Exception,
            Reason = $"多标签策略 UseLatest：选用最新创建任务 {selected.TaskCode}，舍弃 {discarded.Count} 个任务。"
        };
    }

    private static MultiLabelDecisionResult DecideAsException(List<string> allTaskCodes, string barcode)
    {
        return new MultiLabelDecisionResult
        {
            IsMultiLabel = true,
            IsDecisionMade = false,
            DiscardedTaskCodes = allTaskCodes,
            RecommendedStatus = BusinessTaskStatus.Exception,
            Reason = $"多标签策略 MarkException：条码 {barcode} 存在 {allTaskCodes.Count} 个活跃任务，无法自动决策，需人工介入。"
        };
    }
}

