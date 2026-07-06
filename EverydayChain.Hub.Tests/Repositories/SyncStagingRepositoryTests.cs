using EverydayChain.Hub.Infrastructure.Repositories;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncStagingRepositoryTests
{
    [Fact]
    public async Task BulkInsertAsync_ShouldKeepCaseInsensitiveRowDictionary()
    {
        var repository = new SyncStagingRepository();
        var batchId = "batch-1";
        /// <summary>
        /// 存储当前字段值。
        /// </summary>
        const int pageNo = 1;
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows =
        [
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DocNo"] = "DOC-001",
                ["AddTime"] = new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Local),
            },
        ];

        await repository.BulkInsertAsync(
            batchId,
            pageNo,
            rows,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var storedRows = await repository.GetPageRowsAsync(batchId, pageNo, CancellationToken.None);
        var storedRow = Assert.Single(storedRows);
        Assert.True(storedRow.TryGetValue("DOCNO", out var docNo));
        Assert.Equal("DOC-001", docNo);
        Assert.True(storedRow.TryGetValue("addtime", out var addTime));
        Assert.Equal(new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Local), addTime);
    }
}

