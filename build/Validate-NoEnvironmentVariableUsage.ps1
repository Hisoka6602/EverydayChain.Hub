param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRootDirectory
)

$ErrorActionPreference = "Stop"

$script:ViolationMessages = New-Object System.Collections.Generic.List[string]
$script:ViolationLimit = 200
$script:CurrentScriptPath = [System.IO.Path]::GetFullPath($MyInvocation.MyCommand.Path)
$script:NormalizedRepositoryRootDirectory = [System.IO.Path]::GetFullPath($RepositoryRootDirectory.Trim('"'))

function Read-FileLinesUtf8 {
    param(
        [string]$FilePath
    )

    return [System.IO.File]::ReadAllLines($FilePath, [System.Text.Encoding]::UTF8)
}

function Get-RelativePathText {
    param(
        [string]$BaseDirectory,
        [string]$TargetPath
    )

    $normalizedBaseDirectory = [System.IO.Path]::GetFullPath($BaseDirectory)
    if (-not $normalizedBaseDirectory.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $normalizedBaseDirectory += [System.IO.Path]::DirectorySeparatorChar
    }

    $baseUri = New-Object System.Uri($normalizedBaseDirectory)
    $targetUri = New-Object System.Uri([System.IO.Path]::GetFullPath($TargetPath))

    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '\')
}

function Add-Violation {
    param(
        [string]$FilePath,
        [int]$LineNumber,
        [string]$Message
    )

    $relativePath = Get-RelativePathText -BaseDirectory $script:NormalizedRepositoryRootDirectory -TargetPath $FilePath
    $script:ViolationMessages.Add("$relativePath($LineNumber): $Message")
}

function Get-TargetFiles {
    param(
        [string]$RootDirectory
    )

    $supportedExtensions = @(
        ".csproj",
        ".props",
        ".targets",
        ".ps1"
    )

    Get-ChildItem -LiteralPath $RootDirectory -Recurse -File | Where-Object {
        $fullPath = [System.IO.Path]::GetFullPath($_.FullName)
        if ($fullPath -eq $script:CurrentScriptPath) {
            return $false
        }

        if ($fullPath -match "[\\/](bin|obj|\.git|\.agents|\.codex)[\\/]") {
            return $false
        }

        if ($supportedExtensions -contains $_.Extension) {
            return $true
        }

        return $_.Name -eq "launchSettings.json"
    }
}

function Test-EnvironmentVariableUsage {
    param(
        [string]$FilePath,
        [string[]]$Lines
    )

    $rules = @(
        @{
            Pattern = '\bEnvironment\s*\.\s*GetEnvironmentVariable\s*\('
            Message = 'Environment.GetEnvironmentVariable is forbidden. Use appsettings, User Secrets, or command-line arguments instead.'
        },
        @{
            Pattern = '\bEnvironment\s*\.\s*GetEnvironmentVariables\s*\('
            Message = 'Environment.GetEnvironmentVariables is forbidden.'
        },
        @{
            Pattern = '\bEnvironment\s*\.\s*SetEnvironmentVariable\s*\('
            Message = 'Environment.SetEnvironmentVariable is forbidden.'
        },
        @{
            Pattern = '\bEnvironment\s*\.\s*ExpandEnvironmentVariables\s*\('
            Message = 'Environment.ExpandEnvironmentVariables is forbidden.'
        },
        @{
            Pattern = '\bAddEnvironmentVariables\s*\('
            Message = 'ConfigurationBuilder.AddEnvironmentVariables is forbidden.'
        },
        @{
            Pattern = '\[System\.Environment\]::(GetEnvironmentVariable|GetEnvironmentVariables|SetEnvironmentVariable|ExpandEnvironmentVariables)\b'
            Message = 'PowerShell environment variable APIs are forbidden.'
        },
        @{
            Pattern = '\$env:[A-Za-z_][A-Za-z0-9_]*'
            Message = 'PowerShell environment variable access is forbidden.'
        },
        @{
            Pattern = '"environmentVariables"\s*:'
            Message = 'launchSettings environmentVariables are forbidden.'
        }
    )

    for ($lineIndex = 0; $lineIndex -lt $Lines.Length; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        $lineText = $Lines[$lineIndex]

        foreach ($rule in $rules) {
            if ($lineText -match $rule.Pattern) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message $rule.Message
                break
            }
        }
    }
}

$targetFiles = @(Get-TargetFiles -RootDirectory $script:NormalizedRepositoryRootDirectory)

foreach ($targetFile in $targetFiles) {
    Test-EnvironmentVariableUsage -FilePath $targetFile.FullName -Lines (Read-FileLinesUtf8 -FilePath $targetFile.FullName)
}

if ($script:ViolationMessages.Count -gt 0) {
    [Console]::Error.WriteLine("Environment variable usage validation failed.")

    $displayCount = [Math]::Min($script:ViolationMessages.Count, $script:ViolationLimit)
    for ($index = 0; $index -lt $displayCount; $index++) {
        [Console]::Error.WriteLine($script:ViolationMessages[$index])
    }

    if ($script:ViolationMessages.Count -gt $script:ViolationLimit) {
        [Console]::Error.WriteLine("... and {0} more violations." -f ($script:ViolationMessages.Count - $script:ViolationLimit))
    }

    exit 1
}

[Console]::WriteLine("Environment variable usage validation passed.")
