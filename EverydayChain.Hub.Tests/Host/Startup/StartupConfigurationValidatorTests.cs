using EverydayChain.Hub.Host.Startup;
using Microsoft.Extensions.Configuration;

namespace EverydayChain.Hub.Tests.Host.Startup;

public sealed class StartupConfigurationValidatorTests
{
    [Fact]
    public void Validate_ShouldThrow_WhenShardingConnectionStringIsMissing()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["SyncJob:Tables:0:Enabled"] = "false",
            ["WmsFeedback:Enabled"] = "false",
            ["FeedbackCompensationJob:Enabled"] = "false"
        });

        var action = () => StartupConfigurationValidator.Validate(configuration);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("Sharding.ConnectionString", ex.Message);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenShardingConnectionStringIsPlaceholder()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Sharding:ConnectionString"] = "Server=.;Password=__SET_VIA_SHARDING__CONNECTIONSTRING__",
            ["SyncJob:Tables:0:Enabled"] = "false",
            ["WmsFeedback:Enabled"] = "false",
            ["FeedbackCompensationJob:Enabled"] = "false"
        });

        var action = () => StartupConfigurationValidator.Validate(configuration);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("Sharding.ConnectionString", ex.Message);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenOracleIsRequiredButConnectionStringIsPlaceholder()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Sharding:ConnectionString"] = "Server=.;Database=Hub;User Id=sa;Password=real;",
            ["Oracle:ConnectionString"] = "Data Source=127.0.0.1:1521/ORCL;User Id=__SET_VIA_ORACLE__CONNECTIONSTRING__;Password=__SET_VIA_ORACLE__CONNECTIONSTRING__;",
            ["SyncJob:Tables:0:Enabled"] = "true"
        });

        var action = () => StartupConfigurationValidator.Validate(configuration);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("Oracle.ConnectionString", ex.Message);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenOracleIsNotRequired()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Sharding:ConnectionString"] = "Server=.;Database=Hub;User Id=sa;Password=real;",
            ["Oracle:ConnectionString"] = "Data Source=127.0.0.1:1521/ORCL;User Id=__SET_VIA_ORACLE__CONNECTIONSTRING__;Password=__SET_VIA_ORACLE__CONNECTIONSTRING__;",
            ["SyncJob:Tables:0:Enabled"] = "false",
            ["WmsFeedback:Enabled"] = "false",
            ["FeedbackCompensationJob:Enabled"] = "false"
        });

        StartupConfigurationValidator.Validate(configuration);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenLogCleanupConfigIsInvalid()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Sharding:ConnectionString"] = "Server=.;Database=Hub;User Id=sa;Password=real;",
            ["SyncJob:Tables:0:Enabled"] = "false",
            ["WmsFeedback:Enabled"] = "false",
            ["FeedbackCompensationJob:Enabled"] = "false",
            ["LogCleanup:RetentionDays"] = "0"
        });

        var action = () => StartupConfigurationValidator.Validate(configuration);

        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("LogCleanup.RetentionDays", ex.Message);
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
