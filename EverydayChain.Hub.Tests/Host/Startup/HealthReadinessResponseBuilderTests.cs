using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Host.Startup;
using Microsoft.AspNetCore.Http;

namespace EverydayChain.Hub.Tests.Host.Startup;

public sealed class HealthReadinessResponseBuilderTests
{
    [Fact]
    public void Build_ShouldReturnOk_WhenAllDatabasesAreAvailable()
    {
        var readiness = HealthReadinessResponseBuilder.Build(
            new DatabaseConnectivitySnapshot
            {
                CheckedAtLocal = new DateTime(2026, 7, 9, 21, 30, 0),
                LocalSqlServer = new DatabaseEndpointConnectivityState
                {
                    DatabaseName = "LocalSqlServer",
                    IsAvailable = true,
                    Description = "LocalSqlServer available"
                },
                Oracle = new DatabaseEndpointConnectivityState
                {
                    DatabaseName = "Oracle",
                    IsAvailable = true,
                    Description = "Oracle available"
                }
            },
            new ApiWarmupStateSnapshot(
                true,
                false,
                true,
                "Completed",
                "Warmup completed.",
                new DateTime(2026, 7, 9, 21, 29, 0),
                new DateTime(2026, 7, 9, 21, 29, 2)));

        Assert.Equal(StatusCodes.Status200OK, readiness.StatusCode);
        Assert.True(readiness.Response.IsSuccess);
        Assert.NotNull(readiness.Response.Data);
        Assert.True(readiness.Response.Data!.CanServeApiRequests);
        Assert.True(readiness.Response.Data.AllDatabasesAvailable);
        Assert.True(readiness.Response.Data.Oracle.IsAvailable);
        Assert.True(readiness.Response.Data.ApiWarmup.IsCompleted);
        Assert.Equal("已完成", readiness.Response.Data.ApiWarmup.Stage);
    }

    [Fact]
    public void Build_ShouldReturnServiceUnavailable_WhenOracleIsUnavailable()
    {
        var readiness = HealthReadinessResponseBuilder.Build(new DatabaseConnectivitySnapshot
        {
            CheckedAtLocal = new DateTime(2026, 7, 9, 21, 35, 0),
            LocalSqlServer = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "LocalSqlServer",
                IsAvailable = true,
                Description = "LocalSqlServer available"
            },
            Oracle = new DatabaseEndpointConnectivityState
            {
                DatabaseName = "Oracle",
                IsAvailable = false,
                Description = "Oracle unavailable"
            }
        });

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, readiness.StatusCode);
        Assert.False(readiness.Response.IsSuccess);
        Assert.NotNull(readiness.Response.Data);
        Assert.True(readiness.Response.Data!.CanServeApiRequests);
        Assert.False(readiness.Response.Data.AllDatabasesAvailable);
        Assert.False(readiness.Response.Data.Oracle.IsAvailable);
        Assert.False(readiness.Response.Data.ApiWarmup.HasStarted);
    }
}
