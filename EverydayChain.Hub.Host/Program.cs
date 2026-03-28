using EverydayChain.Hub.Host;
using EverydayChain.Hub.Infrastructure.DependencyInjection;
using NLog.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddNLog();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
