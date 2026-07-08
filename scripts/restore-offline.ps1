param(
    [string]$Solution = "EverydayChain.Hub.sln"
)

$ErrorActionPreference = "Stop"

$userProfileDirectory = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile)
$packageCache = Join-Path $userProfileDirectory ".nuget\packages"
$vsOffline = "C:\Program Files (x86)\Microsoft SDKs\NuGetPackages"

if (-not (Test-Path $packageCache)) {
    throw "NuGet package cache was not found: $packageCache"
}

$restoreSources = @($packageCache)
if (Test-Path $vsOffline) {
    $restoreSources += $vsOffline
}

dotnet restore $Solution `
    --packages $packageCache `
    -p:RestoreSources="$($restoreSources -join ';')" `
    -p:RestoreIgnoreFailedSources=true
