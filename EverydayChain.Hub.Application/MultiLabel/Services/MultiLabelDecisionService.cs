using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.MultiLabel.Abstractions;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.MultiLabel;
using EverydayChain.Hub.Domain.Options;
using Microsoft.Extensions.Logging;

namespace EverydayChain.Hub.Application.MultiLabel.Services;

/// <summary>
/// 多标签决策服务实现，识别同一条码关联多个业务任务的场景，并按配置策略输出处置结论。
/// </summary>
public sealed class MultiLabelDecisionService : IMultiLabelDecisionService
{
    /// <summary>业务任务仓储。</summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>异常规则配置。</summary>
    private readonly ExceptionRuleOptions _options;

    /// <summary>日志记录器。</summary>
    private readonly ILogger<MultiLabelDecisionService> _logger;

    /// <summary>
    /// 初始化多标签决策服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="options">异常规则配置。</param>
    /// <param name="logger">日志记录器。</param>
    public MultiLabelDecisionService(
        IBusinessTaskRepository businessTaskRepository,
        ExceptionRuleOptions options,
        ILogger<MultiLabelDecisionService> logger)
    {
        _businessTaskRepository = businessTaskRepository;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 对给定条码的所有关联业务任务执行多标签决策。
    /// 步骤：0. 检查规则开关；1. 校验条码；2. 查询关联任务；3. 单任务直接返回非多标签结论；4. 按策略决策；5. 返回结论。
    /// </summary>
    /// <param name="barcode">条码文本，不能为空白。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>多标签决策结果。</returns>
    public async Task<MultiLabelDecisionResult> DecideAsync(string barcode, CancellationToken ct)
    {
        // 步骤 0：检查总开关与多标签规则开关。
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

        // 步骤 1：校验条码。
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

        // 步骤 2：查询该条码关联的所有活跃业务任务（非终态）。
        var activeTasks = await _businessTaskRepository.FindActiveByBarcodeAsync(trimmedBarcode, ct);

        // 步骤 3：单任务场景直接返回非多标签结论。
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

        // 步骤 4：按策略决策。
        var allTaskCodes = activeTasks.Select(t => t.TaskCode).ToList();
        return _options.MultiLabel.Strategy switch
        {
            "UseFirst" => DecideByFirst(activeTasks, allTaskCodes),
            "UseLatest" => DecideByLatest(activeTasks, allTaskCodes),
            _ => DecideAsException(allTaskCodes, trimmedBarcode)
        };
    }

    /// <summary>
    /// 策略：选用创建时间最早的任务，舍弃其余任务。
    /// </summary>
    private static MultiLabelDecisionResult DecideByFirst(
        IReadOnlyList<Domain.Aggregates.BusinessTaskAggregate.BusinessTaskEntity> tasks,
        List<string> allTaskCodes)
    {
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
    /// 策略：选用创建时间最晚的任务，舍弃其余任务。
    /// </summary>
    private static MultiLabelDecisionResult DecideByLatest(
        IReadOnlyList<Domain.Aggregates.BusinessTaskAggregate.BusinessTaskEntity> tasks,
        List<string> allTaskCodes)
    {
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

    /// <summary>
    /// 策略：将所有关联任务标记为异常，无法自动决策。
    /// </summary>
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
