namespace EverydayChain.Hub.Domain.Abstractions;

/// <summary>
/// 具有主键的实体基础接口。
/// </summary>
/// <typeparam name="TPrimaryKey">主键类型。</typeparam>
public interface IEntity<TPrimaryKey>
{
    /// <summary>
    /// 实体唯一主键。
    /// </summary>
    TPrimaryKey Id { get; set; }
}
