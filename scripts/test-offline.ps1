param(
    [string]$Project = "EverydayChain.Hub.Tests\EverydayChain.Hub.Tests.csproj"
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptRoot "restore-offline.ps1") -Solution "EverydayChain.Hub.sln"

dotnet test $Project `
    --no-restore `
    -m:1 `
    -nr:false `
    -p:UseSharedCompilation=false
