param(
    [string]$Solution = "EverydayChain.Hub.sln"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptRoot "restore-offline.ps1") -Solution $Solution

$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE ".nuget\packages"

dotnet build $Solution `
    --no-restore `
    -m:1 `
    -nr:false `
    -p:UseSharedCompilation=false
