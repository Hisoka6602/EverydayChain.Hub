using NLog;
using NLog.Extensions.Logging;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Infrastructure.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddNLog();
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<SyncBackgroundWorker>();
builder.Services.AddHostedService<RetentionBackgroundWorker>();
#if !DEBUG
var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
if (isWindows) {
    builder.Services.AddWindowsService();
}
else if (isLinux) {
    builder.Services.AddSystemd();
}
#endif
var host = builder.Build();
try {
    host.Run();
}
finally {
    // 确保应用退出时 NLog 将所有缓冲日志全部落盘后再释放资源。
    LogManager.Shutdown();
}
