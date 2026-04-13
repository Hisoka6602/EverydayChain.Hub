using NLog.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;
using EverydayChain.Hub.Host.Workers;
using Microsoft.OpenApi.Models;
using EverydayChain.Hub.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddNLog();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo {
        Title = "EverydayChain Hub 对外接口",
        Version = "v1",
        Description = "提供扫描上传、请求格口与落格回传的 API 骨架能力。"
    });

    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
    if (File.Exists(xmlPath)) {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});
builder.Services.AddHostedService<AutoMigrationHostedService>();
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
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
try {
    app.Run();
}
finally {
    // 确保应用退出时 NLog 将所有缓冲日志全部落盘后再释放资源。
    NLog.LogManager.Shutdown();
}
