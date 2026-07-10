using System.Collections.Concurrent;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Abstractions.Services;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义 ApiWarmupServiceTests 类型。
/// </summary>
public sealed class ApiWarmupServiceTests
{
    [Fact]
    public async Task WarmupAsync_ShouldContinueAndWarmAllQueryChains_WhenSingleStepThrows()
    {
        var globalService = new ThrowingGlobalDashboardQueryService();
        var dockService = new RecordingDockDashboardQueryService();
        var waveService = new RecordingWaveQueryService();
        var businessTaskReadService = new RecordingBusinessTaskReadService();
        var recirculationQueryService = new RecordingRecirculationQueryService();
        var sortingReportQueryService = new RecordingSortingReportQueryService();
        var boxTrackingQueryService = new RecordingBoxTrackingQueryService();
        var chuteQueryService = new RecordingChuteQueryService();
        var exportCatalogQueryService = new RecordingExportCatalogQueryService();
        var retentionCleanupQueryService = new RecordingRetentionCleanupQueryService();
        var service = new ApiWarmupService(
            globalService,
            dockService,
            waveService,
            businessTaskReadService,
            recirculationQueryService,
            sortingReportQueryService,
            boxTrackingQueryService,
            chuteQueryService,
            exportCatalogQueryService,
            retentionCleanupQueryService,
            NullLogger<ApiWarmupService>.Instance);

        await service.WarmupAsync(CancellationToken.None);

        Assert.Equal(2, dockService.Requests.Count);
        Assert.Contains(dockService.Requests, request => string.IsNullOrWhiteSpace(request.WaveCode));
        Assert.Contains(dockService.Requests, request => string.Equals(request.WaveCode, "WARMUP", StringComparison.Ordinal));

        Assert.Equal(1, waveService.CurrentQueryCount);
        Assert.Equal(1, waveService.OptionsQueryCount);
        Assert.Equal(1, waveService.SummaryQueryCount);
        Assert.Equal(1, waveService.ZonesQueryCount);
        Assert.Equal(1, waveService.ListQueryCount);
        Assert.Equal(1, waveService.DetailsQueryCount);
        Assert.Equal(1, waveService.CleanupQueryCount);

        Assert.Equal(2, businessTaskReadService.TaskRequests.Count);
        Assert.Contains(
            businessTaskReadService.TaskRequests,
            request => request.LastCreatedTimeLocal.HasValue && request.LastId.HasValue);
        Assert.Single(businessTaskReadService.ExceptionRequests);
        Assert.Single(businessTaskReadService.RecirculationRequests);

        Assert.Equal(1, recirculationQueryService.QueryCount);
        Assert.Equal(1, sortingReportQueryService.QueryCount);

        Assert.Equal(2, boxTrackingQueryService.Requests.Count);
        Assert.Contains(boxTrackingQueryService.Requests, request => string.IsNullOrWhiteSpace(request.OrderId));
        Assert.Contains(boxTrackingQueryService.Requests, request => string.Equals(request.OrderId, "WARMUP-ORDER", StringComparison.Ordinal));

        Assert.Equal(1, chuteQueryService.QueryCount);
        Assert.Equal(1, exportCatalogQueryService.QueryCount);
        Assert.Equal(1, retentionCleanupQueryService.QueryCount);
    }

    /// <summary>
    /// 定义 ThrowingGlobalDashboardQueryService 类型。
    /// </summary>
    private sealed class ThrowingGlobalDashboardQueryService : IGlobalDashboardQueryService
    {
        public Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Warmup failure.");
        }
    }

    /// <summary>
    /// 定义 RecordingDockDashboardQueryService 类型。
    /// </summary>
    private sealed class RecordingDockDashboardQueryService : IDockDashboardQueryService
    {
        /// <summary>
        /// 获取预热请求集合。
        /// </summary>
        public ConcurrentBag<DockDashboardQueryRequest> Requests { get; } = [];

        public Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new DockDashboardQueryResult());
        }
    }

    /// <summary>
    /// 定义 RecordingWaveQueryService 类型。
    /// </summary>
    private sealed class RecordingWaveQueryService : IWaveQueryService
    {
        /// <summary>
        /// 存储 _currentQueryCount 字段。
        /// </summary>
        private int _currentQueryCount;

        /// <summary>
        /// 存储 _optionsQueryCount 字段。
        /// </summary>
        private int _optionsQueryCount;

        /// <summary>
        /// 存储 _summaryQueryCount 字段。
        /// </summary>
        private int _summaryQueryCount;

        /// <summary>
        /// 存储 _zonesQueryCount 字段。
        /// </summary>
        private int _zonesQueryCount;

        /// <summary>
        /// 存储 _listQueryCount 字段。
        /// </summary>
        private int _listQueryCount;

        /// <summary>
        /// 存储 _detailsQueryCount 字段。
        /// </summary>
        private int _detailsQueryCount;

        /// <summary>
        /// 存储 _cleanupQueryCount 字段。
        /// </summary>
        private int _cleanupQueryCount;

        /// <summary>
        /// 获取 CurrentQueryCount。
        /// </summary>
        public int CurrentQueryCount => _currentQueryCount;

        /// <summary>
        /// 获取 OptionsQueryCount。
        /// </summary>
        public int OptionsQueryCount => _optionsQueryCount;

        /// <summary>
        /// 获取 SummaryQueryCount。
        /// </summary>
        public int SummaryQueryCount => _summaryQueryCount;

        /// <summary>
        /// 获取 ZonesQueryCount。
        /// </summary>
        public int ZonesQueryCount => _zonesQueryCount;

        /// <summary>
        /// 获取 ListQueryCount。
        /// </summary>
        public int ListQueryCount => _listQueryCount;

        /// <summary>
        /// 获取 DetailsQueryCount。
        /// </summary>
        public int DetailsQueryCount => _detailsQueryCount;

        /// <summary>
        /// 获取 CleanupQueryCount。
        /// </summary>
        public int CleanupQueryCount => _cleanupQueryCount;

        public Task<CurrentWaveQueryResult> QueryCurrentAsync(CurrentWaveQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _currentQueryCount);
            return Task.FromResult(new CurrentWaveQueryResult());
        }

        public Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _optionsQueryCount);
            return Task.FromResult(new WaveOptionsQueryResult());
        }

        public Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _summaryQueryCount);
            return Task.FromResult<WaveSummaryQueryResult?>(null);
        }

        public Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _zonesQueryCount);
            return Task.FromResult<WaveZoneQueryResult?>(null);
        }

        public Task<string> ExportZonesCsvAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<WaveListQueryResult> QueryListAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _listQueryCount);
            return Task.FromResult(new WaveListQueryResult());
        }

        public Task<string> ExportListCsvAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<WaveCleanupQueryResult> QueryCleanupWaveAsync(string? waveCode, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _cleanupQueryCount);
            return Task.FromResult(new WaveCleanupQueryResult());
        }

        public Task<WaveDetailQueryResult> QueryDetailsAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _detailsQueryCount);
            return Task.FromResult(new WaveDetailQueryResult());
        }

        public Task<string> ExportDetailsCsvAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }
    }

    /// <summary>
    /// 定义 RecordingBusinessTaskReadService 类型。
    /// </summary>
    private sealed class RecordingBusinessTaskReadService : IBusinessTaskReadService
    {
        /// <summary>
        /// 获取任务查询请求集合。
        /// </summary>
        public ConcurrentBag<BusinessTaskQueryRequest> TaskRequests { get; } = [];

        /// <summary>
        /// 获取异常件查询请求集合。
        /// </summary>
        public ConcurrentBag<BusinessTaskQueryRequest> ExceptionRequests { get; } = [];

        /// <summary>
        /// 获取回流件查询请求集合。
        /// </summary>
        public ConcurrentBag<BusinessTaskQueryRequest> RecirculationRequests { get; } = [];

        public Task<BusinessTaskQueryResult> QueryTasksAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
        {
            TaskRequests.Add(request);
            return Task.FromResult(new BusinessTaskQueryResult());
        }

        public Task<BusinessTaskQueryResult> QueryExceptionsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
        {
            ExceptionRequests.Add(request);
            return Task.FromResult(new BusinessTaskQueryResult());
        }

        public Task<BusinessTaskQueryResult> QueryRecirculationsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
        {
            RecirculationRequests.Add(request);
            return Task.FromResult(new BusinessTaskQueryResult());
        }
    }

    /// <summary>
    /// 定义 RecordingRecirculationQueryService 类型。
    /// </summary>
    private sealed class RecordingRecirculationQueryService : IRecirculationQueryService
    {
        /// <summary>
        /// 存储 _queryCount 字段。
        /// </summary>
        private int _queryCount;

        /// <summary>
        /// 获取 QueryCount。
        /// </summary>
        public int QueryCount => _queryCount;

        public Task<RecirculationSummaryQueryResult> QuerySummaryAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _queryCount);
            return Task.FromResult(new RecirculationSummaryQueryResult());
        }

        public Task<string> ExportCsvAsync(RecirculationSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }
    }

    /// <summary>
    /// 定义 RecordingSortingReportQueryService 类型。
    /// </summary>
    private sealed class RecordingSortingReportQueryService : ISortingReportQueryService
    {
        /// <summary>
        /// 存储 _queryCount 字段。
        /// </summary>
        private int _queryCount;

        /// <summary>
        /// 获取 QueryCount。
        /// </summary>
        public int QueryCount => _queryCount;

        public Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _queryCount);
            return Task.FromResult(new SortingReportQueryResult());
        }

        public Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }
    }

    /// <summary>
    /// 定义 RecordingBoxTrackingQueryService 类型。
    /// </summary>
    private sealed class RecordingBoxTrackingQueryService : IBoxTrackingQueryService
    {
        /// <summary>
        /// 获取请求集合。
        /// </summary>
        public ConcurrentBag<BoxTrackingQueryRequest> Requests { get; } = [];

        public Task<BoxTrackingQueryResult> QueryAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new BoxTrackingQueryResult());
        }

        public Task<IReadOnlyList<BoxTrackingItem>> QueryAllAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult<IReadOnlyList<BoxTrackingItem>>([]);
        }
    }

    /// <summary>
    /// 定义 RecordingChuteQueryService 类型。
    /// </summary>
    private sealed class RecordingChuteQueryService : IChuteQueryService
    {
        /// <summary>
        /// 存储 _queryCount 字段。
        /// </summary>
        private int _queryCount;

        /// <summary>
        /// 获取 QueryCount。
        /// </summary>
        public int QueryCount => _queryCount;

        public Task<ChuteResolveApplicationResult> ExecuteAsync(ChuteResolveApplicationRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _queryCount);
            return Task.FromResult(new ChuteResolveApplicationResult());
        }
    }

    /// <summary>
    /// 定义 RecordingExportCatalogQueryService 类型。
    /// </summary>
    private sealed class RecordingExportCatalogQueryService : IExportCatalogQueryService
    {
        /// <summary>
        /// 存储 _queryCount 字段。
        /// </summary>
        private int _queryCount;

        /// <summary>
        /// 获取 QueryCount。
        /// </summary>
        public int QueryCount => _queryCount;

        public Task<ExportCatalogQueryResult> QueryAsync(ExportCatalogQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _queryCount);
            return Task.FromResult(new ExportCatalogQueryResult());
        }
    }

    /// <summary>
    /// 定义 RecordingRetentionCleanupQueryService 类型。
    /// </summary>
    private sealed class RecordingRetentionCleanupQueryService : IRetentionCleanupQueryService
    {
        /// <summary>
        /// 存储 _queryCount 字段。
        /// </summary>
        private int _queryCount;

        /// <summary>
        /// 获取 QueryCount。
        /// </summary>
        public int QueryCount => _queryCount;

        public Task<RetentionCleanupAuditQueryResult> QueryAsync(RetentionCleanupAuditQueryRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _queryCount);
            return Task.FromResult(new RetentionCleanupAuditQueryResult());
        }
    }
}
