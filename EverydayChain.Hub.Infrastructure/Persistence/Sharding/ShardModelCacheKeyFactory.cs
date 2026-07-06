using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 定义当前类型。
/// </summary>
public class ShardModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) => (context.GetType(), TableSuffixScope.CurrentSuffix ?? string.Empty, designTime);
}

