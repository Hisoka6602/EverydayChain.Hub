param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$RequestPath,

    [string]$RequestBodyJson = "",

    [string]$RequestBodyFile = "",

    [int]$Concurrency = 32,

    [int]$RequestsPerWorker = 200,

    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"

function Get-RequestBodyText {
    param(
        [string]$InlineJson,
        [string]$FilePath
    )

    if (-not [string]::IsNullOrWhiteSpace($FilePath)) {
        return [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)
    }

    return $InlineJson
}

function Get-PercentileValue {
    param(
        [double[]]$Values,
        [double]$Percentile
    )

    if ($Values.Length -eq 0) {
        return 0
    }

    $index = [Math]::Ceiling(($Percentile / 100.0) * $Values.Length) - 1
    if ($index -lt 0) {
        $index = 0
    }

    if ($index -ge $Values.Length) {
        $index = $Values.Length - 1
    }

    return [Math]::Round($Values[$index], 2, [MidpointRounding]::AwayFromZero)
}

function Format-DoubleValue {
    param(
        [double]$Value
    )

    return [Math]::Round($Value, 2, [MidpointRounding]::AwayFromZero)
}

$requestBody = Get-RequestBodyText -InlineJson $RequestBodyJson -FilePath $RequestBodyFile
$targetUrl = $BaseUrl.TrimEnd('/') + "/" + $RequestPath.TrimStart('/')
$latencies = [System.Collections.Concurrent.ConcurrentBag[double]]::new()
$statusCodes = [System.Collections.Concurrent.ConcurrentBag[int]]::new()
$failures = [System.Collections.Concurrent.ConcurrentBag[string]]::new()
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds([Math]::Max(1, $TimeoutSeconds))
$suiteStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$tasks = New-Object 'System.Collections.Generic.List[System.Threading.Tasks.Task]'

for ($workerIndex = 0; $workerIndex -lt $Concurrency; $workerIndex++) {
    $currentWorkerIndex = $workerIndex
    $tasks.Add([System.Threading.Tasks.Task]::Run([Action]{
        for ($requestIndex = 0; $requestIndex -lt $RequestsPerWorker; $requestIndex++) {
            $requestStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                $content = [System.Net.Http.StringContent]::new($requestBody, [System.Text.Encoding]::UTF8, "application/json")
                $response = $client.PostAsync($targetUrl, $content).GetAwaiter().GetResult()
                $requestStopwatch.Stop()
                $latencies.Add($requestStopwatch.Elapsed.TotalMilliseconds)
                $statusCodes.Add([int]$response.StatusCode)
                if (-not $response.IsSuccessStatusCode) {
                    $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    $failures.Add(("工作线程 {0} 的第 {1} 次请求返回状态码 {2}。响应体：{3}" -f ($currentWorkerIndex + 1), ($requestIndex + 1), [int]$response.StatusCode, $responseBody))
                }

                $response.Dispose()
                $content.Dispose()
            }
            catch {
                $requestStopwatch.Stop()
                $latencies.Add($requestStopwatch.Elapsed.TotalMilliseconds)
                $failures.Add(("工作线程 {0} 的第 {1} 次请求发生异常：{2}" -f ($currentWorkerIndex + 1), ($requestIndex + 1), $_.Exception.Message))
            }
        }
    }))
}

[System.Threading.Tasks.Task]::WaitAll($tasks.ToArray())
$suiteStopwatch.Stop()
$client.Dispose()

$orderedLatencies = $latencies.ToArray() | Sort-Object
$orderedStatusCodes = $statusCodes.ToArray() | Sort-Object
$totalRequests = $Concurrency * $RequestsPerWorker
$successCount = $orderedStatusCodes.Count({ $_ -ge 200 -and $_ -lt 300 })
$failureCount = $failures.Count
$elapsedSeconds = [Math]::Max($suiteStopwatch.Elapsed.TotalSeconds, 0.001)
$throughput = $totalRequests / $elapsedSeconds
$averageLatency = if ($orderedLatencies.Count -eq 0) { 0 } else { ($orderedLatencies | Measure-Object -Average).Average }

Write-Host ""
Write-Host "查询压测结果"
Write-Host ("目标地址: {0}" -f $targetUrl)
Write-Host ("并发数: {0}" -f $Concurrency)
Write-Host ("每线程请求数: {0}" -f $RequestsPerWorker)
Write-Host ("总请求数: {0}" -f $totalRequests)
Write-Host ("成功数: {0}" -f $successCount)
Write-Host ("失败数: {0}" -f $failureCount)
Write-Host ("总耗时(秒): {0}" -f (Format-DoubleValue -Value $suiteStopwatch.Elapsed.TotalSeconds))
Write-Host ("吞吐量(请求/秒): {0}" -f (Format-DoubleValue -Value $throughput))
Write-Host ("平均耗时(毫秒): {0}" -f (Format-DoubleValue -Value $averageLatency))
Write-Host ("P95(毫秒): {0}" -f (Get-PercentileValue -Values $orderedLatencies -Percentile 95))
Write-Host ("P99(毫秒): {0}" -f (Get-PercentileValue -Values $orderedLatencies -Percentile 99))

if ($orderedStatusCodes.Length -gt 0) {
    Write-Host ""
    Write-Host "状态码分布"
    $orderedStatusCodes
        | Group-Object
        | Sort-Object Name
        | ForEach-Object {
            Write-Host ("{0}: {1}" -f $_.Name, $_.Count)
        }
}

if ($failureCount -gt 0) {
    Write-Host ""
    Write-Host "失败明细"
    $failures.ToArray() | Select-Object -First 20 | ForEach-Object { Write-Host $_ }
}
