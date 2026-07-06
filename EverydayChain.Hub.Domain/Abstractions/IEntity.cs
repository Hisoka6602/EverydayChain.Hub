namespace EverydayChain.Hub.Domain.Abstractions;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IEntity<TPrimaryKey>
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    TPrimaryKey Id { get; set; }
}

