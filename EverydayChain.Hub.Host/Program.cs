using NLog.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;
using EverydayChain.Hub.Application.Abstractions.Infrastructure;
using EverydayChain.Hub.Host.Workers;
using EverydayChain.Hub.Domain.Options;
using EverydayChain.Hub.Host.Middlewares;
using EverydayChain.Hub.Host.Contracts.Responses;
using EverydayChain.Hub.Host.Startup;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using EverydayChain.Hub.Infrastructure.DependencyInjection;
using EverydayChain.Hub.Host.Swagger;
using EverydayChain.Hub.Host.Services;

// 存储默认的请求参数校验失败提示。
const string DefaultValidationFailureMessage = "请求参数校验失败。";

// 存储空请求体校验失败提示。
const string EmptyRequestBodyValidationMessage = "请求体不能为空，且请求头 Content-Type 必须为 application/json。";

// 存储本地时间格式校验失败提示。
const string DateTimeFormatValidationMessage = "时间字段格式无效，请使用本地时间格式 yyyy-MM-dd HH:mm:ss 或 yyyy-MM-dd HH:mm:ss.fff，禁止使用 Z 或时区偏移。";

var environmentName = ResolveEnvironmentName(args);

var builderOptions = new WebApplicationOptions
{
    Args = args,
    EnvironmentName = environmentName
};
var builder = WebApplication.CreateBuilder(builderOptions);
var environmentConfigFileName = $"appsettings.{builder.Environment.EnvironmentName}.json";
var environmentConfigFilePath = Path.Combine(builder.Environment.ContentRootPath, environmentConfigFileName);
var readOnlySyncConfigFileName = "appsettings.ReadOnlySync.json";
var readOnlySyncConfigFilePath = Path.Combine(builder.Environment.ContentRootPath, readOnlySyncConfigFileName);
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(environmentConfigFileName, optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true);
if (args.Length > 0)
{
    builder.Configuration.AddCommandLine(args);
}
StartupConfigurationValidator.Validate(builder.Configuration);
var logger = NLog.LogManager.GetCurrentClassLogger();
var oracleSection = builder.Configuration.GetSection(OracleOptions.SectionName);
var oracleOptions = oracleSection.Get<OracleOptions>() ?? new OracleOptions();
var syncJobSection = builder.Configuration.GetSection(SyncJobOptions.SectionName);
var syncJobOptions = syncJobSection.Get<SyncJobOptions>() ?? new SyncJobOptions();
var wmsFeedbackSection = builder.Configuration.GetSection(WmsFeedbackOptions.SectionName);
var wmsFeedbackOptions = wmsFeedbackSection.Get<WmsFeedbackOptions>() ?? new WmsFeedbackOptions();
var feedbackCompensationSection = builder.Configuration.GetSection(FeedbackCompensationJobOptions.SectionName);
var feedbackCompensationOptions = feedbackCompensationSection.Get<FeedbackCompensationJobOptions>() ?? new FeedbackCompensationJobOptions();
var environmentConfigFileExists = File.Exists(environmentConfigFilePath);
var readOnlySyncConfigFileExists = File.Exists(readOnlySyncConfigFilePath);
logger.Info(
    "应用启动配置摘要。EnvironmentName={EnvironmentName}, ContentRootPath={ContentRootPath}, EnvironmentConfigFile={EnvironmentConfigFile}, EnvironmentConfigFileExists={EnvironmentConfigFileExists}, ReadOnlySyncConfigFileExists={ReadOnlySyncConfigFileExists}, OracleReadOnly={OracleReadOnly}, OracleDatabase={OracleDatabase}, OracleDatabaseMode={OracleDatabaseMode}, SyncEnabledTableCount={SyncEnabledTableCount}, WmsFeedbackEnabled={WmsFeedbackEnabled}, FeedbackCompensationEnabled={FeedbackCompensationEnabled}",
    builder.Environment.EnvironmentName,
    builder.Environment.ContentRootPath,
    environmentConfigFileName,
    environmentConfigFileExists,
    readOnlySyncConfigFileExists,
    oracleOptions.ReadOnly,
    oracleOptions.Database,
    oracleOptions.DatabaseMode,
    syncJobOptions.Tables.Count(table => table.Enabled),
    wmsFeedbackOptions.Enabled,
    feedbackCompensationOptions.Enabled);
foreach (var warningMessage in StartupEnvironmentDiagnostics.GetWarnings(builder.Environment.EnvironmentName, readOnlySyncConfigFileExists))
{
    logger.Warn(
        "启动环境保护提示。WarningMessage={WarningMessage}, CurrentEnvironment={CurrentEnvironment}, ReadOnlySyncConfigFile={ReadOnlySyncConfigFile}, SuggestedLaunchProfile={SuggestedLaunchProfile}, SuggestedArgs={SuggestedArgs}",
        warningMessage,
        builder.Environment.EnvironmentName,
        readOnlySyncConfigFileName,
        "EverydayChain.Hub.Host.ReadOnlySync",
        "--environment ReadOnlySync");
}
var webEndpointSection = builder.Configuration.GetSection(WebEndpointOptions.SectionName);
var webEndpointOptions = webEndpointSection.Get<WebEndpointOptions>() ?? new WebEndpointOptions();
if (!webEndpointSection.Exists())
{
    logger.Warn("WebEndpoint 配置节缺失，已使用默认配置。");
}
builder.Services.Configure<WebEndpointOptions>(webEndpointSection);
var logCleanupSection = builder.Configuration.GetSection(LogCleanupOptions.SectionName);
if (!logCleanupSection.Exists())
{
    logger.Warn("LogCleanup 配置节缺失，已使用默认配置。");
}
builder.Services.Configure<LogCleanupOptions>(logCleanupSection);

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
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});
builder.Services.AddSingleton<IDiskSpaceProbe, DriveInfoDiskSpaceProbe>();
builder.Services.AddSingleton<LogFileMaintenanceService>();
builder.Services.AddSingleton<IApiWarmupState, ApiWarmupState>();
builder.Services.AddHttpClient<IApiEndpointWarmupService, ApiEndpointWarmupService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(Math.Min(requestTimeoutSeconds, 30));
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
builder.Services.AddHostedService<LogCleanupStartupGuardHostedService>();
builder.Services.AddHostedService<LogCleanupHostedService>();
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
app.UseMiddleware<DatabaseConnectivityMiddleware>();
var shouldEnableSwagger = false;
string? rootEntryPath = null;
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
    rootEntryPath = string.IsNullOrEmpty(swaggerRoutePrefix)
        ? "/"
        : $"/{swaggerRoutePrefix}";

    app.UseSwagger(options => {
        options.RouteTemplate = swaggerJsonRoute;
    });
    app.UseSwaggerUI(options => {
        options.RoutePrefix = swaggerRoutePrefix;
        options.SwaggerEndpoint(swaggerJsonEndpoint, $"{swaggerOptions.Title} {swaggerOptions.Version}");
    });
}
app.MapGet("/", () =>
{
    // 步骤：优先将根路径指向 Swagger 首页，避免 API 宿主根路径直接返回 404。
    if (!string.IsNullOrWhiteSpace(rootEntryPath))
    {
        return Results.Redirect(rootEntryPath);
    }

    return Results.Json(new
    {
        message = "EverydayChain Hub 后端服务正在运行。",
        swagger = "当前环境未启用 Swagger，请直接访问 API 端点。"
    });
});
app.MapGet("/health/live", () =>
{
    return Results.Json(ApiResponse<object>.Success(new
    {
        status = "Live",
        checkedAtLocal = DateTime.Now
    }, "服务存活。"));
});
app.MapGet("/health/ready", async (IDatabaseConnectivityService databaseConnectivityService, IApiWarmupState apiWarmupState, HttpContext httpContext) =>
{
    var snapshot = await databaseConnectivityService.GetSnapshotAsync(httpContext.RequestAborted);
    var readiness = HealthReadinessResponseBuilder.Build(snapshot, apiWarmupState.GetSnapshot());
    return Results.Json(readiness.Response, statusCode: readiness.StatusCode);
});
app.MapControllers();
try {
    app.Run();
}
finally {
    NLog.LogManager.Shutdown();
}

// 步骤：标准化 Swagger 路由前缀。
static string NormalizeSwaggerRoutePrefix(string? path) {
    // 步骤：执行 NormalizeSwaggerRoutePrefix 方法的核心处理流程。
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
    // 步骤：执行 NormalizeValidationMessage 方法的核心处理流程。
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

// 步骤：仅允许通过命令行显式指定环境名，避免依赖环境变量。
static string ResolveEnvironmentName(string[] args) {
    var environmentName = TryGetCommandLineValue(args, "environment");
    if (string.IsNullOrWhiteSpace(environmentName)) {
        return Environments.Production;
    }

    return environmentName.Trim();
}

// 步骤：同时兼容 `--key value` 与 `--key=value` 两种命令行写法。
static string? TryGetCommandLineValue(string[] args, string key) {
    // 步骤：逐个扫描参数，并统一按去前缀后的键名匹配不同命令行格式。
    var normalizedKey = key.Trim().TrimStart('-');
    for (var index = 0; index < args.Length; index++) {
        var argument = args[index];
        var separatorIndex = argument.IndexOf('=');
        if (separatorIndex >= 0) {
            var candidateKey = argument[..separatorIndex].Trim().TrimStart('-');
            if (string.Equals(candidateKey, normalizedKey, StringComparison.OrdinalIgnoreCase)) {
                return argument[(separatorIndex + 1)..];
            }
        }

        var candidateName = argument.Trim().TrimStart('-');
        if (string.Equals(candidateName, normalizedKey, StringComparison.OrdinalIgnoreCase)
            && index + 1 < args.Length) {
            return args[index + 1];
        }
    }

    return null;
}

