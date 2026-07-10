param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [int]$TimeoutSeconds = 60,

    [int]$WarmupWaitSeconds = 240,

    [string]$OutputJson = ""
)

$ErrorActionPreference = "Stop"
$normalizedBaseUrl = $BaseUrl.TrimEnd('/')

function Format-LocalTimeText {
    param(
        [datetime]$Value
    )

    return $Value.ToString("yyyy-MM-dd HH:mm:ss")
}

function ConvertTo-CompressedJson {
    param(
        [hashtable]$Data
    )

    return ($Data | ConvertTo-Json -Depth 10 -Compress)
}

function Try-ParseJson {
    param(
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $null
    }

    try {
        return $Text | ConvertFrom-Json -Depth 20
    }
    catch {
        return $null
    }
}

function Get-JsonValue {
    param(
        $Object,
        [string[]]$Path
    )

    $current = $Object
    foreach ($segment in $Path) {
        if ($null -eq $current) {
            return $null
        }

        if ($current -is [System.Collections.IList] -and $segment -match '^\d+$') {
            $index = [int]$segment
            if ($index -ge $current.Count) {
                return $null
            }

            $current = $current[$index]
            continue
        }

        $property = $current.PSObject.Properties[$segment]
        if ($null -eq $property) {
            return $null
        }

        $current = $property.Value
    }

    return $current
}

function Read-WebExceptionBody {
    param(
        [System.Net.WebException]$Exception
    )

    if ($null -eq $Exception.Response) {
        return $Exception.Message
    }

    $response = $Exception.Response
    $stream = $response.GetResponseStream()
    if ($null -eq $stream) {
        return $Exception.Message
    }

    $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::UTF8)
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
        $response.Dispose()
    }
}

function Invoke-EndpointRequest {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Body,
        [string]$ContentType
    )

    $uri = $normalizedBaseUrl + $Path
    $parameters = @{
        Uri = $uri
        Method = $Method
        TimeoutSec = $TimeoutSeconds
        MaximumRedirection = 5
        UseBasicParsing = $true
        ErrorAction = "Stop"
    }

    if ($Method -ne "GET" -and -not [string]::IsNullOrEmpty($Body)) {
        $parameters["Body"] = $Body
        $parameters["ContentType"] = $ContentType
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest @parameters
        $stopwatch.Stop()
        $preview = if ($null -ne $response.Content) { [string]$response.Content } else { "" }
        if ($preview.Length -gt 512) {
            $preview = $preview.Substring(0, 512)
        }

        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            ElapsedMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2, [System.MidpointRounding]::AwayFromZero)
            Preview = $preview
        }
    }
    catch [System.Net.WebException] {
        $stopwatch.Stop()
        $preview = Read-WebExceptionBody -Exception $_.Exception
        if ($preview.Length -gt 512) {
            $preview = $preview.Substring(0, 512)
        }

        $statusCode = 0
        if ($null -ne $_.Exception.Response -and $null -ne $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }

        return [pscustomobject]@{
            StatusCode = $statusCode
            ElapsedMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2, [System.MidpointRounding]::AwayFromZero)
            Preview = $preview
        }
    }
}

function Wait-ApiWarmupCompleted {
    param(
        [int]$MaxWaitSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($MaxWaitSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $result = Invoke-EndpointRequest -Method "GET" -Path "/health/ready" -Body "" -ContentType "application/json"
        $json = Try-ParseJson -Text $result.Preview
        if ($null -ne $json -and $null -ne $json.data -and $null -ne $json.data.apiWarmup -and $json.data.apiWarmup.isCompleted -eq $true) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "API warmup did not complete within $MaxWaitSeconds seconds."
}

Wait-ApiWarmupCompleted -MaxWaitSeconds $WarmupWaitSeconds

$now = Get-Date
$startTimeLocal = Format-LocalTimeText -Value $now.AddHours(-24)
$endTimeLocal = Format-LocalTimeText -Value $now

$discoveryCurrent = Invoke-EndpointRequest -Method "POST" -Path "/api/v1/waves/current" -Body (ConvertTo-CompressedJson @{
    startTimeLocal = $startTimeLocal
    endTimeLocal = $endTimeLocal
}) -ContentType "application/json"
$discoveryOptions = Invoke-EndpointRequest -Method "POST" -Path "/api/v1/waves/options" -Body (ConvertTo-CompressedJson @{
    startTimeLocal = $startTimeLocal
    endTimeLocal = $endTimeLocal
}) -ContentType "application/json"
$discoveryTasks = Invoke-EndpointRequest -Method "POST" -Path "/api/v1/business-query/tasks" -Body (ConvertTo-CompressedJson @{
    startTimeLocal = $startTimeLocal
    endTimeLocal = $endTimeLocal
    pageNumber = 1
    pageSize = 100
}) -ContentType "application/json"

$currentJson = Try-ParseJson -Text $discoveryCurrent.Preview
$optionsJson = Try-ParseJson -Text $discoveryOptions.Preview
$tasksJson = Try-ParseJson -Text $discoveryTasks.Preview

$waveCode = Get-JsonValue -Object $currentJson -Path @("data", "waveCode")
if ([string]::IsNullOrWhiteSpace($waveCode)) {
    $waveCode = Get-JsonValue -Object $optionsJson -Path @("data", "waveOptions", "0", "waveCode")
}
if ([string]::IsNullOrWhiteSpace($waveCode)) {
    $waveCode = Get-JsonValue -Object $tasksJson -Path @("data", "items", "0", "waveCode")
}
if ([string]::IsNullOrWhiteSpace($waveCode)) {
    $waveCode = "WARMUP"
}

$taskCode = Get-JsonValue -Object $tasksJson -Path @("data", "items", "0", "taskCode")
if ([string]::IsNullOrWhiteSpace($taskCode)) {
    $taskCode = "WARMUP-TASK"
}

$barcode = Get-JsonValue -Object $tasksJson -Path @("data", "items", "0", "barcode")
if ([string]::IsNullOrWhiteSpace($barcode)) {
    $barcode = Get-JsonValue -Object $currentJson -Path @("data", "barcode")
}
if ([string]::IsNullOrWhiteSpace($barcode)) {
    $barcode = "WARMUP-BARCODE"
}

$orderId = Get-JsonValue -Object $tasksJson -Path @("data", "items", "0", "orderId")

$cases = @(
    @{ Name = "Root"; Method = "GET"; Path = "/"; Body = ""; ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "HealthLive"; Method = "GET"; Path = "/health/live"; Body = ""; ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "HealthReady"; Method = "GET"; Path = "/health/ready"; Body = ""; ContentType = "application/json"; ExpectedStatusCodes = @(200, 503) },
    @{ Name = "BusinessTaskSeedManual"; Method = "POST"; Path = "/api/v1/business-task-seed/manual"; Body = (ConvertTo-CompressedJson @{ targetTableName = "business_tasks"; barcodes = @() }); ContentType = "application/json"; ExpectedStatusCodes = @(400) },
    @{ Name = "BusinessQueryTasks"; Method = "POST"; Path = "/api/v1/business-query/tasks"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; pageNumber = 1; pageSize = 100 }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "BusinessQueryExceptions"; Method = "POST"; Path = "/api/v1/business-query/exceptions"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; pageNumber = 1; pageSize = 100 }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "BusinessQueryRecirculations"; Method = "POST"; Path = "/api/v1/business-query/recirculations"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; pageNumber = 1; pageSize = 100 }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "BoxTrackingQuery"; Method = "POST"; Path = "/api/v1/box-tracking/query"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; boxId = $barcode; orderId = $orderId; pageNumber = 1; pageSize = 100 }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "BoxTrackingExportCsv"; Method = "POST"; Path = "/api/v1/box-tracking/export/csv"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; boxId = $barcode; orderId = $orderId }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "BoxTrackingExportXlsx"; Method = "POST"; Path = "/api/v1/box-tracking/export/xlsx"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; boxId = $barcode; orderId = $orderId }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "ExportsCatalog"; Method = "POST"; Path = "/api/v1/exports/catalog"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "DropFeedbackConfirm"; Method = "POST"; Path = "/api/v1/drop-feedback/confirm"; Body = (ConvertTo-CompressedJson @{ taskCode = "WARMUP-TASK-NOT-FOUND"; barcode = "WARMUP-BARCODE-NOT-FOUND"; actualChuteCode = "WARMUP-CHUTE"; dropTimeLocal = $endTimeLocal; isSuccess = $true }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "DockDashboardOverview"; Method = "POST"; Path = "/api/v1/dock-dashboard/overview"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "DockDashboardExportCsv"; Method = "POST"; Path = "/api/v1/dock-dashboard/export/csv"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "DockDashboardExportXlsx"; Method = "POST"; Path = "/api/v1/dock-dashboard/export/xlsx"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "DashboardOverview"; Method = "POST"; Path = "/api/v1/dashboard/overview"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "DashboardExportCsv"; Method = "POST"; Path = "/api/v1/dashboard/export/csv"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "DashboardExportXlsx"; Method = "POST"; Path = "/api/v1/dashboard/export/xlsx"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "DashboardSync"; Method = "POST"; Path = "/api/v1/dashboard/sync"; Body = "{"; ContentType = "application/json"; ExpectedStatusCodes = @(400) },
    @{ Name = "ChuteResolve"; Method = "POST"; Path = "/api/v1/chute/resolve"; Body = (ConvertTo-CompressedJson @{ taskCode = $taskCode; barcode = $barcode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "RecirculationsSummary"; Method = "POST"; Path = "/api/v1/recirculations/summary"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; sortOrder = "Most" }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "RecirculationsDetails"; Method = "POST"; Path = "/api/v1/recirculations/details"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; pageNumber = 1; pageSize = 100 }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "RecirculationsSummaryExportCsv"; Method = "POST"; Path = "/api/v1/recirculations/summary/export/csv"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; sortOrder = "Most" }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "RecirculationsSummaryExportXlsx"; Method = "POST"; Path = "/api/v1/recirculations/summary/export/xlsx"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; sortOrder = "Most" }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "RetentionCleanupsQuery"; Method = "POST"; Path = "/api/v1/retention-cleanups/query"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; pageNumber = 1; pageSize = 100 }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "ScanUpload"; Method = "POST"; Path = "/api/v1/scan/upload"; Body = (ConvertTo-CompressedJson @{ barcodes = @(); deviceCode = "WARMUP-DEVICE"; scanTimeLocal = $endTimeLocal; traceId = "WARMUP-TRACE" }); ContentType = "application/json"; ExpectedStatusCodes = @(400) },
    @{ Name = "ReportsQuery"; Method = "POST"; Path = "/api/v1/reports/query"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "ReportsExportCsv"; Method = "POST"; Path = "/api/v1/reports/export/csv"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "ReportsExportXlsx"; Method = "POST"; Path = "/api/v1/reports/export/xlsx"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WavesCurrent"; Method = "POST"; Path = "/api/v1/waves/current"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WavesOptions"; Method = "POST"; Path = "/api/v1/waves/options"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WavesSummary"; Method = "POST"; Path = "/api/v1/waves/summary"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200, 400) },
    @{ Name = "WavesZones"; Method = "POST"; Path = "/api/v1/waves/zones"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200, 400) },
    @{ Name = "WavesZonesExportCsv"; Method = "POST"; Path = "/api/v1/waves/zones/export/csv"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200, 400) },
    @{ Name = "WavesZonesExportXlsx"; Method = "POST"; Path = "/api/v1/waves/zones/export/xlsx"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200, 400) },
    @{ Name = "WavesList"; Method = "POST"; Path = "/api/v1/waves/list"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WavesListExportCsv"; Method = "POST"; Path = "/api/v1/waves/list/export/csv"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WavesListExportXlsx"; Method = "POST"; Path = "/api/v1/waves/list/export/xlsx"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WavesDetails"; Method = "POST"; Path = "/api/v1/waves/details"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WavesDetailsExportCsv"; Method = "POST"; Path = "/api/v1/waves/details/export/csv"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WavesDetailsExportXlsx"; Method = "POST"; Path = "/api/v1/waves/details/export/xlsx"; Body = (ConvertTo-CompressedJson @{ startTimeLocal = $startTimeLocal; endTimeLocal = $endTimeLocal; waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WaveCleanupQuery"; Method = "POST"; Path = "/api/v1/wave-cleanup/query"; Body = (ConvertTo-CompressedJson @{ waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WaveCleanupDryRun"; Method = "POST"; Path = "/api/v1/wave-cleanup/dry-run"; Body = (ConvertTo-CompressedJson @{ waveCode = $waveCode }); ContentType = "application/json"; ExpectedStatusCodes = @(200) },
    @{ Name = "WaveCleanupExecute"; Method = "POST"; Path = "/api/v1/wave-cleanup/execute"; Body = (ConvertTo-CompressedJson @{ waveCode = "" }); ContentType = "application/json"; ExpectedStatusCodes = @(400) }
)

$results = foreach ($case in $cases) {
    $first = Invoke-EndpointRequest -Method $case.Method -Path $case.Path -Body $case.Body -ContentType $case.ContentType
    $second = Invoke-EndpointRequest -Method $case.Method -Path $case.Path -Body $case.Body -ContentType $case.ContentType

    [pscustomobject]@{
        Name = $case.Name
        Method = $case.Method
        Path = $case.Path
        ExpectedStatusCodes = ($case.ExpectedStatusCodes -join ",")
        FirstStatusCode = $first.StatusCode
        FirstElapsedMs = $first.ElapsedMs
        SecondStatusCode = $second.StatusCode
        SecondElapsedMs = $second.ElapsedMs
        FirstStatusMatched = $case.ExpectedStatusCodes -contains $first.StatusCode
        SecondStatusMatched = $case.ExpectedStatusCodes -contains $second.StatusCode
        Preview = $first.Preview
    }
}

if (-not [string]::IsNullOrWhiteSpace($OutputJson)) {
    $results | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputJson -Encoding UTF8
}

$results
