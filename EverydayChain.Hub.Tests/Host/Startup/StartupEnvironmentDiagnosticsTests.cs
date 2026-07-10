using EverydayChain.Hub.Host.Startup;

namespace EverydayChain.Hub.Tests.Host.Startup;

public sealed class StartupEnvironmentDiagnosticsTests
{
    [Fact]
    public void GetWarnings_ShouldWarn_WhenReadOnlySyncConfigExistsButEnvironmentIsNotReadOnlySync()
    {
        var warnings = StartupEnvironmentDiagnostics.GetWarnings("Production", readOnlySyncConfigFileExists: true);

        var warning = Assert.Single(warnings);
        Assert.Contains("未启用 ReadOnlySync 环境", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void GetWarnings_ShouldWarn_WhenEnvironmentIsReadOnlySyncButConfigIsMissing()
    {
        var warnings = StartupEnvironmentDiagnostics.GetWarnings("ReadOnlySync", readOnlySyncConfigFileExists: false);

        var warning = Assert.Single(warnings);
        Assert.Contains("未找到 appsettings.ReadOnlySync.json", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void GetWarnings_ShouldReturnEmpty_WhenEnvironmentAndConfigMatchReadOnlySyncUsage()
    {
        var warnings = StartupEnvironmentDiagnostics.GetWarnings("ReadOnlySync", readOnlySyncConfigFileExists: true);

        Assert.Empty(warnings);
    }
}
