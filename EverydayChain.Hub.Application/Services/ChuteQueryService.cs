using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Services;

/// <summary>
/// 请求格口应用服务实现，按条码或任务编码查询任务并返回目标格口。
/// </summary>
public sealed class ChuteQueryService : IChuteQueryService {
    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 条码解析服务。
    /// </summary>
    private readonly IBarcodeParser _barcodeParser;

    /// <summary>
    /// 初始化请求格口应用服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    /// <param name="barcodeParser">条码解析服务。</param>
    public ChuteQueryService(IBusinessTaskRepository businessTaskRepository, IBarcodeParser barcodeParser)
    {
        _businessTaskRepository = businessTaskRepository;
        _barcodeParser = barcodeParser;
    }

    /// <summary>
    /// 按任务编码或条码查询目标格口。
    /// 步骤：1. 优先按任务编码查找；2. 任务编码为空时按条码查找；3. 校验任务状态；4. 优先返回已持久化目标格口；5. 校验条码非空；6. 解析条码中的格口并返回。
    /// </summary>
    /// <param name="request">请求参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>格口解析结果。</returns>
    public async Task<ChuteResolveApplicationResult> ExecuteAsync(ChuteResolveApplicationRequest request, CancellationToken cancellationToken) {
        var normalizedTaskCode = string.IsNullOrWhiteSpace(request.TaskCode) ? null : request.TaskCode.Trim();
        var normalizedBarcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

        // 步骤 1：优先按任务编码查找任务。
        var task = normalizedTaskCode is not null
            ? await _businessTaskRepository.FindByTaskCodeAsync(normalizedTaskCode, cancellationToken)
            : null;

        // 步骤 2：任务编码为空或未命中时，按条码查找任务。
        if (task == null && normalizedBarcode is not null) {
            task = await _businessTaskRepository.FindByBarcodeAsync(normalizedBarcode, cancellationToken);
        }

        // 步骤 3：任务不存在时返回失败。
        if (task == null) {
            return new ChuteResolveApplicationResult {
                IsResolved = false,
                TaskCode = string.Empty,
                ChuteCode = string.Empty,
                Message = $"未找到条码 [{normalizedBarcode}] 或任务编码 [{normalizedTaskCode}] 对应的业务任务。"
            };
        }

        // 步骤 4：校验任务状态，仅已扫描或已落格任务可请求格口。
        if (task.Status != BusinessTaskStatus.Scanned && task.Status != BusinessTaskStatus.Dropped) {
            return new ChuteResolveApplicationResult {
                IsResolved = false,
                TaskCode = task.TaskCode,
                ChuteCode = string.Empty,
                Message = $"任务 [{task.TaskCode}] 当前状态 [{task.Status}] 不允许请求格口，仅已扫描或已落格任务可查询格口。"
            };
        }

        // 步骤 5：优先返回任务已持久化的目标格口，避免重复条码解析。
        var normalizedTargetChuteCode = string.IsNullOrWhiteSpace(task.TargetChuteCode) ? null : task.TargetChuteCode.Trim();
        if (normalizedTargetChuteCode is not null)
        {
            return new ChuteResolveApplicationResult
            {
                IsResolved = true,
                TaskCode = task.TaskCode,
                ChuteCode = normalizedTargetChuteCode,
                Message = $"任务 [{task.TaskCode}] 目标格口已确认：{normalizedTargetChuteCode}。"
            };
        }

        // 步骤 6：从任务条码中解析目标格口；解析失败时返回失败。
        if (string.IsNullOrWhiteSpace(task.Barcode)) {
            return new ChuteResolveApplicationResult {
                IsResolved = false,
                TaskCode = task.TaskCode,
                ChuteCode = string.Empty,
                Message = $"任务 [{task.TaskCode}] 条码为空，无法解析目标格口。"
            };
        }

        var barcodeForResolve = task.Barcode;
        var parseResult = _barcodeParser.Parse(barcodeForResolve);
        if (!parseResult.IsValid) {
            return new ChuteResolveApplicationResult {
                IsResolved = false,
                TaskCode = task.TaskCode,
                ChuteCode = string.Empty,
                Message = $"任务 [{task.TaskCode}] 条码 [{barcodeForResolve}] 未携带受支持的目标格口信息。"
            };
        }

        return new ChuteResolveApplicationResult {
            IsResolved = true,
            TaskCode = task.TaskCode,
            ChuteCode = parseResult.TargetChuteCode,
            Message = $"任务 [{task.TaskCode}] 目标格口已确认：{parseResult.TargetChuteCode}。"
        };
    }
}
