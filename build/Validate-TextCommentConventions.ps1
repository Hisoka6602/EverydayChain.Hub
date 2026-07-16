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
$script:GarbledChars = @(
    [string][char]0xFFFD,
    [string][char]0x951B,
    [string][char]0x9286,
    [string][char]0x951F
)
$script:ForbiddenUnicodePatterns = @(
    '\\u[0-9A-Fa-f]{4}',
    '\\U[0-9A-Fa-f]{8}',
    '\\u\{[0-9A-Fa-f]{1,8}\}'
)

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

    foreach ($garbledChar in $script:GarbledChars) {
        if ($Text.Contains($garbledChar)) {
            return $true
        }
    }

    return $false
}

function Test-ForbiddenUnicodeUsage {
    param(
        [AllowEmptyString()]
        [string]$Text
    )

    if ([string]::IsNullOrEmpty($Text)) {
        return $false
    }

    foreach ($unicodeEscapePattern in $script:ForbiddenUnicodePatterns) {
        if ($Text -match $unicodeEscapePattern) {
            return $true
        }
    }

    return $Text -match '[\p{Cf}]'
}

function Test-PlaceholderComment {
    param(
        [AllowEmptyString()]
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $false
    }

    $normalizedText = $Text.Trim()
    $placeholderPatterns = @(
        '^定义当前类型。?$',
        '^定义当前成员。?$',
        '^执行当前方法。?$',
        '^存储当前字段值。?$',
        '^获取或设置当前属性值。?$',
        '^步骤：按既定流程执行当前方法逻辑。?$'
    )

    foreach ($placeholderPattern in $placeholderPatterns) {
        if ($normalizedText -match $placeholderPattern) {
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

function Get-BlockCommentText {
    param(
        [string]$LineText,
        [string]$StartMarker,
        [string]$EndMarker
    )

    $trimmedLine = $LineText.Trim()
    if ($trimmedLine.StartsWith($StartMarker, [System.StringComparison]::Ordinal)) {
        $commentText = $trimmedLine.Substring($StartMarker.Length)
        $endIndex = $commentText.IndexOf($EndMarker, [System.StringComparison]::Ordinal)
        if ($endIndex -ge 0) {
            $commentText = $commentText.Substring(0, $endIndex)
        }

        return $commentText.Trim()
    }

    $endOnlyIndex = $trimmedLine.IndexOf($EndMarker, [System.StringComparison]::Ordinal)
    if ($endOnlyIndex -ge 0) {
        return $trimmedLine.Substring(0, $endOnlyIndex).Trim()
    }

    return $trimmedLine
}

function Test-RepositoryCommentText {
    param(
        [string]$FilePath,
        [int]$LineNumber,
        [string]$CommentText
    )

    if ([string]::IsNullOrWhiteSpace($CommentText)) {
        return
    }

    if (-not (Test-ChineseText -Text $CommentText)) {
        Add-Violation -FilePath $FilePath -LineNumber $LineNumber -Message "Comments must use Chinese."
    }

    if (Test-GarbledText -Text $CommentText) {
        Add-Violation -FilePath $FilePath -LineNumber $LineNumber -Message "Comments contain garbled text."
    }

    if (Test-ForbiddenUnicodeUsage -Text $CommentText) {
        Add-Violation -FilePath $FilePath -LineNumber $LineNumber -Message "Unicode escape sequences and invisible Unicode characters are forbidden."
    }

    if (Test-PlaceholderComment -Text $CommentText) {
        Add-Violation -FilePath $FilePath -LineNumber $LineNumber -Message "Comments must be meaningful and cannot use placeholder text."
    }
}

$repositoryTextFiles = @(Get-RepositoryTextFiles -RootDirectory $RepositoryRootDirectory)

foreach ($repositoryTextFile in $repositoryTextFiles) {
    $lines = Read-FileLinesUtf8 -FilePath $repositoryTextFile
    $insidePowerShellBlockComment = $false
    $insideXmlBlockComment = $false

    for ($lineIndex = 0; $lineIndex -lt $lines.Length; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        $lineText = $lines[$lineIndex]
        $trimmedLine = $lineText.Trim()

        if ($insidePowerShellBlockComment) {
            $commentText = Get-BlockCommentText -LineText $lineText -StartMarker "<#" -EndMarker "#>"
            Test-RepositoryCommentText -FilePath $repositoryTextFile -LineNumber $lineNumber -CommentText $commentText

            if ($trimmedLine.Contains("#>")) {
                $insidePowerShellBlockComment = $false
            }

            continue
        }

        if ($insideXmlBlockComment) {
            $commentText = Get-BlockCommentText -LineText $lineText -StartMarker "<!--" -EndMarker "-->"
            Test-RepositoryCommentText -FilePath $repositoryTextFile -LineNumber $lineNumber -CommentText $commentText

            if ($trimmedLine.Contains("-->")) {
                $insideXmlBlockComment = $false
            }

            continue
        }

        if ($trimmedLine.StartsWith("<#", [System.StringComparison]::Ordinal)) {
            $commentText = Get-BlockCommentText -LineText $lineText -StartMarker "<#" -EndMarker "#>"
            Test-RepositoryCommentText -FilePath $repositoryTextFile -LineNumber $lineNumber -CommentText $commentText

            if (-not $trimmedLine.Contains("#>")) {
                $insidePowerShellBlockComment = $true
            }

            continue
        }

        if ($trimmedLine.StartsWith("<!--", [System.StringComparison]::Ordinal)) {
            $commentText = Get-BlockCommentText -LineText $lineText -StartMarker "<!--" -EndMarker "-->"
            Test-RepositoryCommentText -FilePath $repositoryTextFile -LineNumber $lineNumber -CommentText $commentText

            if (-not $trimmedLine.Contains("-->")) {
                $insideXmlBlockComment = $true
            }

            continue
        }

        $commentText = Get-CommentText -LineText $lineText
        Test-RepositoryCommentText -FilePath $repositoryTextFile -LineNumber $lineNumber -CommentText $commentText
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
