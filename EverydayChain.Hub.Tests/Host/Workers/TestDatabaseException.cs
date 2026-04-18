using System.Data.Common;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 数据库异常测试桩。
/// </summary>
public sealed class TestDatabaseException : DbException
{
    /// <summary>
    /// 是否瞬态异常。
    /// </summary>
    private readonly bool _isTransient;

    /// <summary>
    /// 初始化数据库异常测试桩。
    /// </summary>
    /// <param name="message">异常消息。</param>
    /// <param name="isTransient">是否瞬态异常。</param>
    public TestDatabaseException(string message, bool isTransient) : base(message)
    {
        _isTransient = isTransient;
    }

    /// <inheritdoc />
    public override bool IsTransient => _isTransient;
}
