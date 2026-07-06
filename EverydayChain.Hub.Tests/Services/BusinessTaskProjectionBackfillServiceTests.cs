using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;
using EverydayChain.Hub.Domain.Sync;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskProjectionBackfillServiceTests
{
    [Fact]
    public async Task PreviewAsync_ShouldSummarizeHistoricalProjectionGaps()
    {
        var start = DateTime.SpecifyKind(new DateTime(2026, 6, 1, 0, 0, 0), DateTimeKind.Local);
        var repository = new InMemoryBusinessTaskRepository();
        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "SKU-001",
            SourceTableCode = "WmsPickToWcs",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "SKU-001",
            Barcode = "SKU-001",
            OrderId = null,
            StoreId = "STORE-001",
            StoreName = "Store Name 001",
            ProductCode = null,
            PickLocation = "A-01-01",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);
        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "SKU-002",
            SourceTableCode = "WmsPickToWcs",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "SKU-002",
            Barcode = "SKU-002",
            OrderId = "ORDER-002",
            StoreId = null,
            StoreName = null,
            ProductCode = "ITEM-002",
            PickLocation = null,
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(2),
            UpdatedTimeLocal = start.AddHours(2)
        }, CancellationToken.None);

        var definition = new SyncTableDefinition
        {
            TableCode = "WmsPickToWcs",
            Enabled = true,
            SourceSchema = "WMS",
            SourceTable = "IDX_PICKTOWCS2",
            CursorColumn = "ADDTIME",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKeyColumn = "SKUID",
            OrderIdColumn = "DOCNO",
            StoreIdColumn = "CONSIGNEEID",
            StoreNameColumn = "MENDIAN",
            ProductCodeColumn = "SKU",
            PickLocationColumn = "LOCATION"
        };
        var service = new BusinessTaskProjectionBackfillService(
            new FakeSyncTaskConfigRepository([definition]),
            repository,
            new FakeOracleSourceReader(new Dictionary<string, IReadOnlyDictionary<string, object?>>()),
            new BusinessTaskProjectionService(),
            NullLogger<BusinessTaskProjectionBackfillService>.Instance);

        var result = await service.PreviewAsync(new BusinessTaskProjectionBackfillPreviewCommand
        {
            StartTimeLocal = start,
            EndTimeLocal = start.AddDays(1),
            TableCode = "WmsPickToWcs"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ProcessedTableCount);
        Assert.Equal(2, result.CandidateCount);
        var table = Assert.Single(result.Tables);
        Assert.Equal("WmsPickToWcs", table.TableCode);
        Assert.Equal(2, table.CandidateCount);
        Assert.Equal(1, table.MissingOrderIdCount);
        Assert.Equal(1, table.MissingStoreIdCount);
        Assert.Equal(1, table.MissingStoreNameCount);
        Assert.Equal(1, table.MissingProductCodeCount);
        Assert.Equal(1, table.MissingPickLocationCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBackfillExtendedFields_FromRemoteRows()
    {
        var start = DateTime.SpecifyKind(new DateTime(2026, 6, 1, 0, 0, 0), DateTimeKind.Local);
        var repository = new InMemoryBusinessTaskRepository();
        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "SKU-001",
            SourceTableCode = "WmsPickToWcs",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKey = "SKU-001",
            Barcode = "SKU-001",
            WaveCode = "WAVE-01",
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var definition = new SyncTableDefinition
        {
            TableCode = "WmsPickToWcs",
            Enabled = true,
            SourceSchema = "WMS",
            SourceTable = "IDX_PICKTOWCS2",
            CursorColumn = "ADDTIME",
            SourceType = BusinessTaskSourceType.FullCase,
            BusinessKeyColumn = "SKUID",
            BarcodeColumn = "SKUID",
            WaveCodeColumn = "WAVENO",
            OrderIdColumn = "DOCNO",
            StoreIdColumn = "CONSIGNEEID",
            StoreNameColumn = "MENDIAN",
            ProductCodeColumn = "SKU",
            PickLocationColumn = "LOCATION"
        };
        var oracleReader = new FakeOracleSourceReader(new Dictionary<string, IReadOnlyDictionary<string, object?>>
        {
            ["WmsPickToWcs::SKU-001"] = new Dictionary<string, object?>
            {
                ["SKUID"] = "SKU-001",
                ["WAVENO"] = "WAVE-01",
                ["DOCNO"] = "ORDER-001",
                ["CONSIGNEEID"] = "STORE-001",
                ["MENDIAN"] = "Store Name 001",
                ["SKU"] = "ITEM-001",
                ["LOCATION"] = "A-01-01",
                ["ADDTIME"] = start.AddMinutes(15)
            }
        });
        var service = new BusinessTaskProjectionBackfillService(
            new FakeSyncTaskConfigRepository([definition]),
            repository,
            oracleReader,
            new BusinessTaskProjectionService(),
            NullLogger<BusinessTaskProjectionBackfillService>.Instance);

        var result = await service.ExecuteAsync(new BusinessTaskProjectionBackfillCommand
        {
            StartTimeLocal = start,
            EndTimeLocal = start.AddDays(1),
            TableCode = "WmsPickToWcs",
            MaxCount = 100,
            BatchSize = 20
        }, CancellationToken.None);

        var reloaded = await repository.FindBySourceTableAndBusinessKeyAsync("WmsPickToWcs", "SKU-001", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ProcessedTableCount);
        Assert.Equal(1, result.CandidateCount);
        Assert.Equal(1, result.RemoteRowCount);
        Assert.Equal(1, result.ProjectedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.MissingRemoteCount);
        Assert.NotNull(reloaded);
        Assert.Equal("ORDER-001", reloaded!.OrderId);
        Assert.Equal("STORE-001", reloaded.StoreId);
        Assert.Equal("Store Name 001", reloaded.StoreName);
        Assert.Equal("ITEM-001", reloaded.ProductCode);
        Assert.Equal("A-01-01", reloaded.PickLocation);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIgnoreFieldsThatSourceTableCannotProvide()
    {
        var start = DateTime.SpecifyKind(new DateTime(2026, 6, 1, 0, 0, 0), DateTimeKind.Local);
        var repository = new InMemoryBusinessTaskRepository();
        await repository.SaveAsync(new BusinessTaskEntity
        {
            TaskCode = "CARTON-001",
            SourceTableCode = "WmsSplitPickToLightCarton",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKey = "CARTON-001",
            Barcode = "CARTON-001",
            OrderId = "ORDER-001",
            StoreId = "STORE-001",
            StoreName = "Store Name 001",
            ProductCode = null,
            PickLocation = null,
            Status = BusinessTaskStatus.Created,
            CreatedTimeLocal = start.AddHours(1),
            UpdatedTimeLocal = start.AddHours(1)
        }, CancellationToken.None);

        var definition = new SyncTableDefinition
        {
            TableCode = "WmsSplitPickToLightCarton",
            Enabled = true,
            SourceSchema = "WMS",
            SourceTable = "IDX_PICKTOLIGHT_CARTON1",
            CursorColumn = "ADDTIME",
            SourceType = BusinessTaskSourceType.Split,
            BusinessKeyColumn = "CARTONNO",
            BarcodeColumn = "CARTONNO",
            OrderIdColumn = "DOCNO",
            StoreIdColumn = "CONSIGNEEID",
            StoreNameColumn = "MENDIAN",
            ProductCodeColumn = null,
            PickLocationColumn = null
        };
        var service = new BusinessTaskProjectionBackfillService(
            new FakeSyncTaskConfigRepository([definition]),
            repository,
            new FakeOracleSourceReader(new Dictionary<string, IReadOnlyDictionary<string, object?>>()),
            new BusinessTaskProjectionService(),
            NullLogger<BusinessTaskProjectionBackfillService>.Instance);

        var result = await service.ExecuteAsync(new BusinessTaskProjectionBackfillCommand
        {
            StartTimeLocal = start,
            EndTimeLocal = start.AddDays(1),
            TableCode = "WmsSplitPickToLightCarton",
            MaxCount = 100,
            BatchSize = 20
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.CandidateCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Empty(result.Tables);
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class FakeSyncTaskConfigRepository(IReadOnlyList<SyncTableDefinition> definitions) : ISyncTaskConfigRepository
    {
        public Task<SyncTableDefinition> GetByTableCodeAsync(string tableCode, CancellationToken ct)
        {
            var definition = definitions.FirstOrDefault(item => string.Equals(item.TableCode, tableCode, StringComparison.OrdinalIgnoreCase));
            if (definition is null)
            {
                throw new InvalidOperationException($"Missing table definition: {tableCode}");
            }

            return Task.FromResult(definition);
        }

        public Task<IReadOnlyList<SyncTableDefinition>> ListEnabledAsync(CancellationToken ct)
        {
            return Task.FromResult(definitions);
        }

        public Task<int> GetMaxParallelTablesAsync(CancellationToken ct)
        {
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class FakeOracleSourceReader(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> rowsByKey) : IOracleSourceReader
    {
        public Task<SyncReadResult> ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlySet<string>> ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 执行当前方法。
        /// </summary>
        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadRowsByBusinessKeysAsync(
            OracleBusinessKeyRowReadRequest request,
            CancellationToken ct)
        {
            // 步骤：按既定流程执行当前方法逻辑。
            IReadOnlyList<IReadOnlyDictionary<string, object?>> rows = request.BusinessKeys
                .Select(key =>
                {
                    rowsByKey.TryGetValue($"{request.TableCode}::{key}", out var row);
                    return row;
                })
                .Where(row => row is not null)
                .Cast<IReadOnlyDictionary<string, object?>>()
                .ToList();
            return Task.FromResult(rows);
        }
    }
}

