param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRootDirectory
)

$ErrorActionPreference = "Stop"

$RepositoryRootDirectory = $RepositoryRootDirectory.Trim('"')
if ([string]::IsNullOrWhiteSpace($RepositoryRootDirectory)) {
    throw "RepositoryRootDirectory is required."
}

$RepositoryRootDirectory = [System.IO.Path]::GetFullPath($RepositoryRootDirectory)

$script:ViolationMessages = New-Object System.Collections.Generic.List[string]
$script:ViolationLimit = 200

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

    $relativePath = Get-RelativePathText -BaseDirectory $RepositoryRootDirectory -TargetPath $FilePath
    $script:ViolationMessages.Add("$relativePath($LineNumber): $Message")
}

function Test-ChineseText {
    param(
        [AllowEmptyString()]
        [string]$Text
    )

    return $Text -match "[\u4E00-\u9FFF]"
}

function Test-GarbledText {
    param(
        [AllowEmptyString()]
        [string]$Text
    )

    $garbledChars = @(
        [string][char]0xFFFD,
        [string][char]0x951B,
        [string][char]0x9286,
        [string][char]0x951F
    )

    foreach ($garbledChar in $garbledChars) {
        if ($Text.Contains($garbledChar)) {
            return $true
        }
    }

    return $false
}

function Get-RepositoryTextFiles {
    param(
        [string]$RootDirectory
    )

    $candidatePaths = New-Object System.Collections.Generic.List[string]
    $staticFiles = @(
        "Directory.Build.props",
        "Directory.Build.targets"
    )

    foreach ($staticFile in $staticFiles) {
        $fullPath = Join-Path $RootDirectory $staticFile
        if (Test-Path $fullPath) {
            $candidatePaths.Add($fullPath)
        }
    }

    $includePatterns = @(
        "build\*.ps1",
        "EverydayChain.Hub.Host\appsettings*.json"
    )

    foreach ($includePattern in $includePatterns) {
        $matchedFiles = Get-ChildItem -Path (Join-Path $RootDirectory $includePattern) -File -ErrorAction SilentlyContinue
        foreach ($matchedFile in $matchedFiles) {
            $candidatePaths.Add($matchedFile.FullName)
        }
    }

    return @($candidatePaths | Sort-Object -Unique)
}

function Get-CommentText {
    param(
        [string]$LineText
    )

    $trimmedLine = $LineText.Trim()
    if ($trimmedLine.StartsWith("//", [System.StringComparison]::Ordinal)) {
        return $trimmedLine.Substring(2).Trim()
    }

    if ($trimmedLine.StartsWith("#", [System.StringComparison]::Ordinal)) {
        return $trimmedLine.Substring(1).Trim()
    }

    if ($trimmedLine.StartsWith("<!--", [System.StringComparison]::Ordinal)) {
        $xmlCommentMatch = [regex]::Match($trimmedLine, "<!--(.*?)-->")
        if ($xmlCommentMatch.Success) {
            return $xmlCommentMatch.Groups[1].Value.Trim()
        }
    }

    return $null
}

$repositoryTextFiles = @(Get-RepositoryTextFiles -RootDirectory $RepositoryRootDirectory)

foreach ($repositoryTextFile in $repositoryTextFiles) {
    $lines = Read-FileLinesUtf8 -FilePath $repositoryTextFile
    for ($lineIndex = 0; $lineIndex -lt $lines.Length; $lineIndex++) {
        $commentText = Get-CommentText -LineText $lines[$lineIndex]
        if ([string]::IsNullOrWhiteSpace($commentText)) {
            continue
        }

        if (-not (Test-ChineseText -Text $commentText)) {
            Add-Violation -FilePath $repositoryTextFile -LineNumber ($lineIndex + 1) -Message "Comments must use Chinese."
        }

        if (Test-GarbledText -Text $commentText) {
            Add-Violation -FilePath $repositoryTextFile -LineNumber ($lineIndex + 1) -Message "Comments contain garbled text."
        }
    }
}

if ($script:ViolationMessages.Count -gt 0) {
    [Console]::Error.WriteLine("Text comment convention validation failed.")

    $displayCount = [Math]::Min($script:ViolationMessages.Count, $script:ViolationLimit)
    for ($index = 0; $index -lt $displayCount; $index++) {
        [Console]::Error.WriteLine($script:ViolationMessages[$index])
    }

    if ($script:ViolationMessages.Count -gt $script:ViolationLimit) {
        [Console]::Error.WriteLine(("... and {0} more violations." -f ($script:ViolationMessages.Count - $script:ViolationLimit)))
    }

    exit 1
}

[Console]::WriteLine("Text comment convention validation passed.")
