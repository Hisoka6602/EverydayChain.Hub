using System.Reflection;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EverydayChain.Hub.Tests.Services;

namespace EverydayChain.Hub.Tests.Repositories;

/// <summary>
/// 定义 OracleSourceReaderTests 类型。
/// </summary>
public class OracleSourceReaderTests
{
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

    private static string InvokeBuildConnectionString(OracleOptions options)
    {
        var method = typeof(OracleSourceReader).GetMethod("BuildConnectionString", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(null, [options]);
        Assert.IsType<string>(result);
        return (string)result;
    }
}

