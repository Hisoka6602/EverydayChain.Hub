using NLog.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Middlewares;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using EverydayChain.Hub.Infrastructure.DependencyInjection;

const string DefaultValidationFailureMessage = "请求参数校验失败。";
const string EmptyRequestBodyValidationMessage = "请求体不能为空，且请求头 Content-Type 必须为 application/json。";
const string DateTimeFormatValidationMessage = "时间字段格式无效，请使用本地时间格式 yyyy-MM-dd HH:mm:ss 或 yyyy-MM-dd HH:mm:ss.fff，禁止使用 Z 或时区偏移。";

var builder = WebApplication.CreateBuilder(args);
var logger = NLog.LogManager.GetCurrentClassLogger();
var webEndpointSection = builder.Configuration.GetSection(WebEndpointOptions.SectionName);
var webEndpointOptions = webEndpointSection.Get<WebEndpointOptions>() ?? new WebEndpointOptions();
if (!webEndpointSection.Exists())
{
    logger.Warn("WebEndpoint 配置节缺失，已使用默认配置。");
}

if (!string.IsNullOrWhiteSpace(webEndpointOptions.Url)) {
    builder.WebHost.UseUrls(webEndpointOptions.Url.Trim());
}

var swaggerSection = builder.Configuration.GetSection(SwaggerOptions.SectionName);
var swaggerOptions = swaggerSection.Get<SwaggerOptions>() ?? new SwaggerOptions();
if (!swaggerSection.Exists())
{
    logger.Warn("Swagger 配置节缺失，已使用默认配置。");
}
builder.Logging.ClearProviders();
builder.Logging.AddNLog();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .ConfigureApiBehaviorOptions(options => {
    options.InvalidModelStateResponseFactory = context => {
        var firstError = DefaultValidationFailureMessage;
        foreach (var modelStateEntry in context.ModelState.Values) {
            foreach (var error in modelStateEntry.Errors) {
                if (!string.IsNullOrWhiteSpace(error.ErrorMessage)) {
                    firstError = error.ErrorMessage;
                    goto BuildResponse;
                }
            }
        }

BuildResponse:
        firstError = NormalizeValidationMessage(firstError);
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
builder.Services.AddHostedService<FeedbackCompensationBackgroundWorker>();
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
app.UseRouting();
app.UseMiddleware<ApiFailureLoggingMiddleware>();
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
    var swaggerRoutePrefix = NormalizeSwaggerRoutePrefix(swaggerOptions.Path);
    var swaggerJsonRoute = string.IsNullOrEmpty(swaggerRoutePrefix)
        ? "{documentName}/swagger.json"
        : $"{swaggerRoutePrefix}/{{documentName}}/swagger.json";
    var swaggerJsonEndpoint = string.IsNullOrEmpty(swaggerRoutePrefix)
        ? "/v1/swagger.json"
        : $"/{swaggerRoutePrefix}/v1/swagger.json";

    app.UseSwagger(options => {
        options.RouteTemplate = swaggerJsonRoute;
    });
    app.UseSwaggerUI(options => {
        options.RoutePrefix = swaggerRoutePrefix;
        options.SwaggerEndpoint(swaggerJsonEndpoint, $"{swaggerOptions.Title} {swaggerOptions.Version}");
    });
}
app.MapControllers();
try {
    app.Run();
}
finally {
    // 确保应用退出时 NLog 将所有缓冲日志全部落盘后再释放资源。
    NLog.LogManager.Shutdown();
}

// 归一化 Swagger 路由前缀。
// path: 配置路径。
// 返回值：路由前缀；根路径返回空字符串。
static string NormalizeSwaggerRoutePrefix(string? path) {
    if (string.IsNullOrWhiteSpace(path)) {
        return "swagger";
    }

    var trimmed = path.Trim();
    if (trimmed == "/") {
        return string.Empty;
    }

    return trimmed.Trim('/').ToLowerInvariant();
}

// 归一化模型绑定错误消息，避免直接暴露底层序列化器实现细节。
// rawMessage: 原始错误文本。
// 返回值：可直接用于响应的统一错误文本。
static string NormalizeValidationMessage(string rawMessage) {
    if (string.IsNullOrWhiteSpace(rawMessage)) {
        return DefaultValidationFailureMessage;
    }

    if (rawMessage.Contains("The request field is required.", StringComparison.OrdinalIgnoreCase)
        || rawMessage.Contains("A non-empty request body is required.", StringComparison.OrdinalIgnoreCase)) {
        return EmptyRequestBodyValidationMessage;
    }

    if (rawMessage.Contains("could not be converted to System.DateTime", StringComparison.OrdinalIgnoreCase)
        || rawMessage.Contains("could not be converted to System.Nullable`1[System.DateTime]", StringComparison.OrdinalIgnoreCase)) {
        return DateTimeFormatValidationMessage;
    }

    return rawMessage;
}
