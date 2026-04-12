using System.Reflection;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Tests.Services;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// OracleSourceReader 连接串构建逻辑测试。
/// </summary>
public class OracleSourceReaderTests
{
    /// <summary>
    /// 连接串为空白时应抛出 InvalidOperationException（连接串不可为空，应快速失败）。
    /// </summary>
    [Fact]
    public void BuildConnectionString_WhenConnectionStringIsBlank_ShouldThrow()
    {
        var options = new OracleOptions
        {
            ConnectionString = " ",
            Database = "ORCL"
        };

        var action = () => InvokeBuildConnectionString(options);
        var exception = Assert.Throws<TargetInvocationException>(action);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// 未配置库名时应保持连接串原样返回。
    /// </summary>
    [Fact]
    public void BuildConnectionString_WhenDatabaseIsBlank_ShouldKeepOriginal()
    {
        var options = new OracleOptions
        {
            ConnectionString = "Data Source=10.0.0.1:1521/OLD;User Id=u;Password=p;",
            Database = " "
        };

        var result = InvokeBuildConnectionString(options);
        Assert.Contains("Data Source=10.0.0.1:1521/OLD", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// EZCONNECT 斜杠格式应可覆写库名。
    /// </summary>
    [Fact]
    public void BuildConnectionString_WhenDataSourceUsesSlash_ShouldOverrideDatabase()
    {
        var options = new OracleOptions
        {
            ConnectionString = "Data Source=10.0.0.1:1521/OLD;User Id=u;Password=p;",
            Database = "NEWDB"
        };

        var result = InvokeBuildConnectionString(options);
        Assert.Contains("Data Source=10.0.0.1:1521/NEWDB", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// EZCONNECT 冒号 SID 格式应可覆写库名。
    /// </summary>
    [Fact]
    public void BuildConnectionString_WhenDataSourceUsesSidStyle_ShouldOverrideDatabase()
    {
        var options = new OracleOptions
        {
            ConnectionString = "Data Source=10.0.0.1:1521:OLD;User Id=u;Password=p;",
            Database = "NEWDB"
        };

        var result = InvokeBuildConnectionString(options);
        Assert.Contains("Data Source=10.0.0.1:1521:NEWDB", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// host:port 且显式 Sid 模式时应按 SID 语法拼接。
    /// </summary>
    [Fact]
    public void BuildConnectionString_WhenDatabaseModeIsSid_ShouldUseSidStyle()
    {
        var options = new OracleOptions
        {
            ConnectionString = "Data Source=10.0.0.1:1521;User Id=u;Password=p;",
            Database = "NEWDB",
            DatabaseMode = "Sid"
        };

        var result = InvokeBuildConnectionString(options);
        Assert.Contains("Data Source=10.0.0.1:1521:NEWDB", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 非法 DatabaseMode 应抛出异常。
    /// </summary>
    [Fact]
    public void BuildConnectionString_WhenDatabaseModeInvalid_ShouldThrow()
    {
        var options = new OracleOptions
        {
            ConnectionString = "Data Source=10.0.0.1:1521;User Id=u;Password=p;",
            Database = "NEWDB",
            DatabaseMode = "Unknown"
        };

        var action = () => InvokeBuildConnectionString(options);
        var exception = Assert.Throws<TargetInvocationException>(action);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// 复杂描述符配置库名覆盖时应抛出异常。
    /// </summary>
    [Fact]
    public void BuildConnectionString_WhenDataSourceIsDescriptor_ShouldThrow()
    {
        var options = new OracleOptions
        {
            ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=10.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=OLD)));User Id=u;Password=p;",
            Database = "NEWDB"
        };

        var action = () => InvokeBuildConnectionString(options);
        var exception = Assert.Throws<TargetInvocationException>(action);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// SourceSchema 为空时应抛出配置异常（不再回退 Oracle.DefaultSchema）。
    /// </summary>
    [Fact]
    public void ResolveSourceSchema_WhenSourceSchemaBlank_ShouldThrow()
    {
        var reader = new OracleSourceReader(
            Options.Create(new OracleOptions
            {
                ConnectionString = "Data Source=10.0.0.1:1521/ORCL;User Id=u;Password=p;",
                Database = "ORCL"
            }),
            new PassThroughDangerZoneExecutor(),
            NullLogger<OracleSourceReader>.Instance);
        var method = typeof(OracleSourceReader).GetMethod("ResolveSourceSchema", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var action = () => method!.Invoke(reader, [""]);
        var exception = Assert.Throws<TargetInvocationException>(action);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    /// <summary>
    /// 通过反射调用私有静态连接串构建方法。
    /// </summary>
    /// <param name="options">Oracle 配置。</param>
    /// <returns>构建后的连接串。</returns>
    private static string InvokeBuildConnectionString(OracleOptions options)
    {
        var method = typeof(OracleSourceReader).GetMethod("BuildConnectionString", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(null, [options]);
        Assert.IsType<string>(result);
        return (string)result;
    }
}
