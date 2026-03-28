using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

public class ShardModelCacheKeyFactory : IModelCacheKeyFactory {
    public object Create(DbContext context, bool designTime) => (context.GetType(), TableSuffixScope.CurrentSuffix ?? string.Empty, designTime);
}
