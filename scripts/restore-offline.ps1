param(
    [string]$Solution = "EverydayChain.Hub.sln"
)

$ErrorActionPreference = "Stop"

$packageCache = Join-Path $env:USERPROFILE ".nuget\packages"
$vsOffline = "C:\Program Files (x86)\Microsoft SDKs\NuGetPackages"

if (-not (Test-Path $packageCache)) {
    throw "NuGet package cache was not found: $packageCache"
}

$restoreSources = @($packageCache)
if (Test-Path $vsOffline) {
    $restoreSources += $vsOffline
}

$env:NUGET_PACKAGES = $packageCache

dotnet restore $Solution `
    --packages $packageCache `
    -p:RestoreSources="$($restoreSources -join ';')" `
    -p:RestoreIgnoreFailedSources=true
