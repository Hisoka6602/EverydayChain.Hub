using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EverydayChain.Hub.Infrastructure.Persistence.Sharding;

/// <summary>
/// 自定义 EF Core 模型缓存键工厂，使不同分表后缀产生独立的模型缓存，避免路由混乱。
/// </summary>
public class ShardModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <summary>
    /// 根据 DbContext 类型、当前分表后缀及设计时标志生成缓存键。
    /// </summary>
    /// <param name="context">当前 DbContext 实例。</param>
    /// <param name="designTime">是否为设计时模式。</param>
    /// <returns>由类型、后缀与设计时标志组成的元组缓存键。</returns>
    public object Create(DbContext context, bool designTime) => (context.GetType(), TableSuffixScope.CurrentSuffix ?? string.Empty, designTime);
}
