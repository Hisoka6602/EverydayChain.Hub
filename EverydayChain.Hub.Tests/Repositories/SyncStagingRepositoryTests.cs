using EverydayChain.Hub.Infrastructure.Repositories;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// SyncStagingRepository 行存储行为测试。
/// </summary>
public class SyncStagingRepositoryTests
{
    /// <summary>
    /// 暂存写入后应保持行字段大小写不敏感访问能力。
    /// </summary>
    [Fact]
    public async Task BulkInsertAsync_ShouldKeepCaseInsensitiveRowDictionary()
    {
        var repository = new SyncStagingRepository();
        var batchId = "batch-1";
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
