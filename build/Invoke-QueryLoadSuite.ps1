param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [int]$Concurrency = 32,

    [int]$RequestsPerWorker = 200,

    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "Invoke-QueryLoadTest.ps1"
$caseDirectory = Join-Path $PSScriptRoot "load-test"

if (-not (Test-Path $scriptPath)) {
    throw "未找到查询压测脚本。"
}

if (-not (Test-Path $caseDirectory)) {
    throw "未找到压测样例目录。"
}

$cases = @(
    @{ Name = "总看板"; RequestPath = "api/v1/dashboard/overview"; BodyFile = "global-dashboard.json" },
    @{ Name = "码头看板"; RequestPath = "api/v1/dock-dashboard/overview"; BodyFile = "dock-dashboard.json" },
    @{ Name = "当前波次"; RequestPath = "api/v1/waves/current"; BodyFile = "waves-current.json" },
    @{ Name = "波次列表"; RequestPath = "api/v1/waves/list"; BodyFile = "waves-list.json" },
    @{ Name = "回流汇总"; RequestPath = "api/v1/recirculations/summary"; BodyFile = "recirculation-summary.json" },
    @{ Name = "箱子追踪"; RequestPath = "api/v1/box-tracking/query"; BodyFile = "box-tracking.json" },
    @{ Name = "分拣报表"; RequestPath = "api/v1/reports/query"; BodyFile = "sorting-report.json" }
)

foreach ($case in $cases) {
    $bodyFilePath = Join-Path $caseDirectory $case.BodyFile
    if (-not (Test-Path $bodyFilePath)) {
        throw ("未找到压测样例文件：{0}" -f $bodyFilePath)
    }

    Write-Host ""
    Write-Host ("================ {0} ================" -f $case.Name)
    & powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File $scriptPath `
        -BaseUrl $BaseUrl `
        -RequestPath $case.RequestPath `
        -RequestBodyFile $bodyFilePath `
        -Concurrency $Concurrency `
        -RequestsPerWorker $RequestsPerWorker `
        -TimeoutSeconds $TimeoutSeconds
}
