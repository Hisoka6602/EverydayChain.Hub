using System.Data.Common;

namespace EverydayChain.Hub.Tests.Host.Workers;

/// <summary>
/// 数据库异常测试桩。
/// </summary>
public sealed class TestDatabaseException : DbException
{
    /// <summary>
    /// 初始化数据库异常测试桩。
    /// </summary>
    /// <param name="message">异常消息。</param>
    public TestDatabaseException(string message) : base(message)
    {
    }
}
