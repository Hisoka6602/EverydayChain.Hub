using NLog.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Middlewares;
using EverydayChain.Hub.Host.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using EverydayChain.Hub.Infrastructure.DependencyInjection;
using EverydayChain.Hub.Host.Swagger;

// 存储默认的请求参数校验失败提示。
const string DefaultValidationFailureMessage = "请求参数校验失败。";

// 存储空请求体校验失败提示。
const string EmptyRequestBodyValidationMessage = "请求体不能为空，且请求头 Content-Type 必须为 application/json。";

// 存储本地时间格式校验失败提示。
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
var configuredTimeout = webEndpointOptions.RequestTimeoutSeconds;
var requestTimeoutSeconds = Math.Clamp(configuredTimeout > 0 ? configuredTimeout : 30, 1, 600);
if (configuredTimeout > 600) {
    logger.Warn(
        "WebEndpoint.RequestTimeoutSeconds 配置值 {Value} 超出约定上限 600，已自动钳制为 600 秒。",
        configuredTimeout);
}

var swaggerSection = builder.Configuration.GetSection(SwaggerOptions.SectionName);
var swaggerOptions = swaggerSection.Get<SwaggerOptions>() ?? new SwaggerOptions();
if (!swaggerSection.Exists())
{
    logger.Warn("Swagger 配置节缺失，已使用默认配置。");
}
builder.Logging.ClearProviders();
builder.Logging.AddNLog();
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});
var efCoreOptsSection = builder.Configuration.GetSection(EfCoreOptions.SectionName);
var efCoreOpts = efCoreOptsSection.Get<EfCoreOptions>() ?? new EfCoreOptions();
if (efCoreOpts.CommandTimeoutSeconds > 600) {
    logger.Warn(
        "EfCore.CommandTimeoutSeconds 配置值 {Value} 超出约定上限 600，已自动钳制为 600 秒。",
        efCoreOpts.CommandTimeoutSeconds);
}
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddNewtonsoftJson()
    .ConfigureApiBehaviorOptions(options => {
    // 步骤：统一覆盖模型校验失败响应体。
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
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(requestTimeoutSeconds)
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
    options.SchemaFilter<BusinessTaskSeedTableSchemaFilter>();
});
builder.Services.AddHostedService<AutoMigrationHostedService>();
builder.Services.AddHostedService<ApiWarmupHostedService>();
builder.Services.AddHostedService<SyncBackgroundWorker>();
builder.Services.AddHostedService<RetentionBackgroundWorker>();
builder.Services.AddHostedService<WmsFeedbackBackgroundWorker>();
builder.Services.AddHostedService<FeedbackCompensationBackgroundWorker>();
builder.Services.AddHostedService<DashboardSnapshotBackgroundWorker>();
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
app.UseRequestTimeouts();
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
    NLog.LogManager.Shutdown();
}

// 步骤：标准化 Swagger 路由前缀。
static string NormalizeSwaggerRoutePrefix(string? path) {
    // 步骤：按既定流程执行当前方法逻辑。
    if (string.IsNullOrWhiteSpace(path)) {
        return "swagger";
    }

    var trimmed = path.Trim();
    if (trimmed == "/") {
        return string.Empty;
    }

    return trimmed.Trim('/').ToLowerInvariant();
}

// 步骤：统一转换模型校验失败提示文案。
static string NormalizeValidationMessage(string rawMessage) {
    // 步骤：按既定流程执行当前方法逻辑。
    if (string.IsNullOrWhiteSpace(rawMessage)) {
        return DefaultValidationFailureMessage;
    }

    if (rawMessage.Contains("The request field is required.", StringComparison.OrdinalIgnoreCase)
        || rawMessage.Contains("A non-empty request body is required.", StringComparison.OrdinalIgnoreCase)) {
        return EmptyRequestBodyValidationMessage;
    }

    var isDateTimeConversionFailure =
        rawMessage.Contains("could not be converted to System.DateTime", StringComparison.OrdinalIgnoreCase)
        || rawMessage.Contains("could not be converted to System.Nullable`1[System.DateTime]", StringComparison.OrdinalIgnoreCase)
        || (rawMessage.Contains("Error converting value", StringComparison.OrdinalIgnoreCase)
            && rawMessage.Contains("to type 'System.DateTime'", StringComparison.OrdinalIgnoreCase))
        || (rawMessage.Contains("Error converting value", StringComparison.OrdinalIgnoreCase)
            && rawMessage.Contains("to type 'System.Nullable`1[System.DateTime]'", StringComparison.OrdinalIgnoreCase))
        || rawMessage.Contains("Could not convert string to DateTime", StringComparison.OrdinalIgnoreCase);
    if (isDateTimeConversionFailure) {
        return DateTimeFormatValidationMessage;
    }

    return rawMessage;
}

