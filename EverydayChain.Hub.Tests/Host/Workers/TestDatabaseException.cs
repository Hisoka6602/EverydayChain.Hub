using System.Data.Common;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class TestDatabaseException : DbException
{
    /// <summary>
    /// 存储当前字段值。
    /// </summary>
    private readonly bool _isTransient;

    public TestDatabaseException(string message, bool isTransient) : base(message)
    {
        _isTransient = isTransient;
    }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public override bool IsTransient => _isTransient;
}

