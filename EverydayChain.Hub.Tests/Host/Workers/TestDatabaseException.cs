using System.Data.Common;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 定义 TestDatabaseException 类型。
/// </summary>
public sealed class TestDatabaseException : DbException
{
    /// <summary>
    /// 存储 _isTransient 字段。
    /// </summary>
    private readonly bool _isTransient;

    public TestDatabaseException(string message, bool isTransient) : base(message)
    {
        _isTransient = isTransient;
    }

    /// <summary>
    /// 获取或设置 IsTransient。
    /// </summary>
    public override bool IsTransient => _isTransient;
}

