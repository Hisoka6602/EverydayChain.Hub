using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Http;

namespace EverydayChain.Hub.Host.Startup;

/// <summary>
/// 负责根据数据库连通性快照和启动预热状态构建就绪检查响应。
/// </summary>
public static class HealthReadinessResponseBuilder
{
    /// <summary>
    /// 构建就绪检查响应。
    /// </summary>
    /// <param name="snapshot">数据库连通性快照。</param>
    /// <param name="warmupSnapshot">启动预热状态快照。</param>
    /// <returns>包含状态码与响应体的就绪检查响应。</returns>
    public static HealthReadinessResponse Build(
        DatabaseConnectivitySnapshot snapshot,
        ApiWarmupStateSnapshot? warmupSnapshot = null)
    {
        var payload = new HealthReadinessPayload
        {
            CanServeApiRequests = snapshot.LocalSqlServer.IsAvailable,
            AllDatabasesAvailable = snapshot.IsAvailable,
            CheckedAtLocal = snapshot.CheckedAtLocal,
            LocalSqlServer = new HealthReadinessDatabaseState
            {
                IsAvailable = snapshot.LocalSqlServer.IsAvailable,
                Description = snapshot.LocalSqlServer.Description
            },
            Oracle = new HealthReadinessDatabaseState
            {
                IsAvailable = snapshot.Oracle.IsAvailable,
                Description = snapshot.Oracle.Description
            },
            ApiWarmup = BuildWarmupState(warmupSnapshot)
        };

        if (snapshot.IsAvailable)
        {
            return new HealthReadinessResponse(
                StatusCodes.Status200OK,
                ApiResponse<HealthReadinessPayload>.Success(payload, "服务就绪。"));
        }

        return new HealthReadinessResponse(
            StatusCodes.Status503ServiceUnavailable,
            ApiResponse<HealthReadinessPayload>.Fail(snapshot.BuildUserMessage(), payload));
    }

    private static HealthReadinessWarmupState BuildWarmupState(ApiWarmupStateSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return new HealthReadinessWarmupState
            {
                HasStarted = false,
                IsRunning = false,
                IsCompleted = false,
                Stage = "未知",
                Message = "启动预热状态不可用。"
            };
        }

        return new HealthReadinessWarmupState
        {
            HasStarted = snapshot.HasStarted,
            IsRunning = snapshot.IsRunning,
            IsCompleted = snapshot.IsCompleted,
            Stage = LocalizeWarmupStage(snapshot.Stage),
            Message = snapshot.Message,
            StartedAtLocal = snapshot.StartedAtLocal,
            CompletedAtLocal = snapshot.CompletedAtLocal
        };
    }

    private static string LocalizeWarmupStage(string? stage)
    {
        return stage switch
        {
            "Pending" => "等待开始",
            "Bootstrap" => "启动初始化",
            "LocalSqlUnavailable" => "本地数据库不可用",
            "DbContextWarmup" => "EF 模型预热",
            "DashboardSnapshotWarmup" => "看板快照预热",
            "QueryServiceWarmup" => "查询服务预热",
            "WaitForApplicationStarted" => "等待 Web 宿主监听",
            "HttpEndpointWarmup" => "HTTP 端点预热",
            "Completed" => "已完成",
            "Cancelled" => "已取消",
            "Failed" => "失败",
            _ => stage ?? string.Empty
        };
    }
}

/// <summary>
/// 表示就绪检查端点的最终输出。
/// </summary>
/// <param name="statusCode">HTTP 状态码。</param>
/// <param name="response">统一包装后的响应体。</param>
public sealed class HealthReadinessResponse(
    int statusCode,
    ApiResponse<HealthReadinessPayload> response)
{
    /// <summary>
    /// 获取当前就绪检查对应的 HTTP 状态码。
    /// </summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>
    /// 获取统一包装后的就绪检查响应体。
    /// </summary>
    public ApiResponse<HealthReadinessPayload> Response { get; } = response;
}

/// <summary>
/// 表示就绪检查返回的详细载荷。
/// </summary>
public sealed class HealthReadinessPayload
{
    /// <summary>
    /// 获取或设置当前是否可以基于本地 SQL Server 对外提供查询接口。
    /// </summary>
    public bool CanServeApiRequests { get; set; }

    /// <summary>
    /// 获取或设置当前是否所有依赖数据库都已就绪。
    /// </summary>
    public bool AllDatabasesAvailable { get; set; }

    /// <summary>
    /// 获取或设置本次连通性快照的检查时间。
    /// </summary>
    public DateTime CheckedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置本地 SQL Server 的连通性状态。
    /// </summary>
    public HealthReadinessDatabaseState LocalSqlServer { get; set; } = new();

    /// <summary>
    /// 获取或设置远端 Oracle 的连通性状态。
    /// </summary>
    public HealthReadinessDatabaseState Oracle { get; set; } = new();

    /// <summary>
    /// 获取或设置启动预热状态。
    /// </summary>
    public HealthReadinessWarmupState ApiWarmup { get; set; } = new();
}

/// <summary>
/// 表示单个数据库端点的连通性状态。
/// </summary>
public sealed class HealthReadinessDatabaseState
{
    /// <summary>
    /// 获取或设置当前数据库端点是否可用。
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// 获取或设置当前数据库端点的描述信息。
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 表示启动预热状态。
/// </summary>
public sealed class HealthReadinessWarmupState
{
    /// <summary>
    /// 获取或设置启动预热是否已经开始。
    /// </summary>
    public bool HasStarted { get; set; }

    /// <summary>
    /// 获取或设置启动预热是否仍在运行中。
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// 获取或设置启动预热是否已经完成。
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 获取或设置当前预热阶段名称。
    /// </summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前预热说明。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置启动预热开始时间。
    /// </summary>
    public DateTime? StartedAtLocal { get; set; }

    /// <summary>
    /// 获取或设置启动预热完成时间。
    /// </summary>
    public DateTime? CompletedAtLocal { get; set; }
}
