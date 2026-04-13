using NLog.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using EverydayChain.Hub.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var logger = NLog.LogManager.GetCurrentClassLogger();
var swaggerSection = builder.Configuration.GetSection(SwaggerOptions.SectionName);
var swaggerOptions = swaggerSection.Get<SwaggerOptions>() ?? new SwaggerOptions();
if (!swaggerSection.Exists()) {
    logger.Warn("Swagger 配置节缺失或绑定失败，已使用默认配置。");
}
builder.Logging.ClearProviders();
builder.Logging.AddNLog();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options => {
    options.InvalidModelStateResponseFactory = context => {
        var firstError = "请求参数校验失败。";
        foreach (var modelStateEntry in context.ModelState.Values) {
            foreach (var error in modelStateEntry.Errors) {
                if (!string.IsNullOrWhiteSpace(error.ErrorMessage)) {
                    firstError = error.ErrorMessage;
                    goto BuildResponse;
                }
            }
        }

BuildResponse:
        return new BadRequestObjectResult(ApiResponse<object>.Fail(firstError));
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo {
        Title = swaggerOptions.Title,
        Version = swaggerOptions.Version,
        Description = swaggerOptions.Description
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
var shouldEnableSwagger = false;
if (app.Environment.IsDevelopment()) {
    shouldEnableSwagger = swaggerOptions.EnableInDevelopment;
}
else if (app.Environment.IsEnvironment("Test")) {
    shouldEnableSwagger = swaggerOptions.EnableInTest;
}
else {
    shouldEnableSwagger = swaggerOptions.EnableInProduction;
}
if (shouldEnableSwagger) {
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
try {
    app.Run();
}
finally {
    // 确保应用退出时 NLog 将所有缓冲日志全部落盘后再释放资源。
    NLog.LogManager.Shutdown();
}
