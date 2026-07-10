using EverydayChain.Hub.SharedKernel.Utilities;

namespace EverydayChain.Hub.Tests.SharedKernel;

public sealed class RuntimeLeaseOwnerIdTests
{
    [Fact]
    public void Create_ShouldReturnParseableOwnerIdWithinColumnLimit()
    {
        var ownerId = RuntimeLeaseOwnerId.Create();

        Assert.True(ownerId.Length <= 64);
        Assert.True(RuntimeLeaseOwnerId.TryParse(ownerId, out var descriptor));
        Assert.Equal(Environment.ProcessId, descriptor.ProcessId);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.MachineName));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Token));
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_ForLegacyBatchStyleOwnerId()
    {
        var legacyOwnerId = Guid.NewGuid().ToString("N");

        Assert.False(RuntimeLeaseOwnerId.TryParse(legacyOwnerId, out _));
    }
}
