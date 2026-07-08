namespace EverydayChain.Hub.Domain.Abstractions;

/// <summary>
/// 定义 IEntity 类型。
/// </summary>
public interface IEntity<TPrimaryKey>
{
    /// <summary>
    /// 获取或设置 Id。
    /// </summary>
    TPrimaryKey Id { get; set; }
}

