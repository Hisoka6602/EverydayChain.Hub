param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDirectory,

    [Parameter(Mandatory = $true)]
    [string]$ProjectName
)

$ErrorActionPreference = "Stop"

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
$script:UtcPatterns = @(
    "DateTime\.UtcNow",
    "DateTimeOffset\.UtcNow",
    "DateTimeKind\.Utc",
    "TimeZoneInfo\.Utc",
    "\.ToUniversalTime\s*\(",
    "\bUtcDateTime\b",
    "DateTimeStyles\.AssumeUniversal",
    "DateTimeStyles\.AdjustToUniversal"
)
$script:EnvironmentVariablePatterns = @(
    '\bEnvironment\s*\.\s*GetEnvironmentVariable\s*\(',
    '\bEnvironment\s*\.\s*GetEnvironmentVariables\s*\(',
    '\bEnvironment\s*\.\s*SetEnvironmentVariable\s*\(',
    '\bEnvironment\s*\.\s*ExpandEnvironmentVariables\s*\(',
    '\bAddEnvironmentVariables\s*\('
)
$script:ThreadPoolStarvationRiskPatterns = @(
    '\bTask\s*\.\s*Run\s*\('
)
$script:FloatingPointPatterns = @(
    '\bdouble\b',
    '\bfloat\b',
    '\bSystem\.Double\b',
    '\bSystem\.Single\b'
)
$script:EnumMemberLinePattern = '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(?:=\s*[^,]+)?\s*,?\s*(?://.*)?$'

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

    $relativePath = Get-RelativePathText -BaseDirectory $ProjectDirectory -TargetPath $FilePath
    $script:ViolationMessages.Add("$relativePath($LineNumber): $Message")
}

function Test-ChineseText {
    param(
        [AllowEmptyString()]
        [string]$Text
    )

    return $Text -match "[\u4E00-\u9FFF]"
}

function Get-DocCommentPlainText {
    param(
        [AllowEmptyString()]
        [string]$Text
    )

    $withoutXmlTags = [regex]::Replace($Text, "<[^>]+>", [string]::Empty)
    return $withoutXmlTags.Trim()
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

function Test-GenericNameOnlyComment {
    param(
        [string]$FilePath,
        [AllowEmptyString()]
        [string]$Text
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $false
    }

    if ($FilePath -notmatch 'EverydayChain\.Hub\.Host[\\/]+Contracts[\\/]') {
        return $false
    }

    $normalizedText = $Text.Trim()
    $weakPatterns = @(
        '^定义\s+[A-Za-z_][A-Za-z0-9_<>,\?\s]*\s+类型。?$',
        '^定义\s+[A-Za-z_][A-Za-z0-9_<>,\?\s]*\s+成员。?$',
        '^执行\s+[A-Za-z_][A-Za-z0-9_<>,\?\s]*\s+方法。?$',
        '^存储\s+[A-Za-z_][A-Za-z0-9_<>,\?\s]*\s+字段。?$',
        '^获取或设置\s+[A-Za-z_][A-Za-z0-9_<>,\?\s]*。?$'
    )

    foreach ($weakPattern in $weakPatterns) {
        if ($normalizedText -match $weakPattern) {
            return $true
        }
    }

    return $false
}

function Get-SourceFiles {
    param(
        [string]$RootDirectory
    )

    $candidateFiles = Get-ChildItem -Path $RootDirectory -Recurse -File -Filter "*.cs"

    foreach ($candidateFile in $candidateFiles) {
        if ($candidateFile.FullName -match "[\\/](bin|obj)[\\/]") {
            continue
        }

        if ($candidateFile.Name -match "(\.Designer|\.g|\.g\.i)\.cs$") {
            continue
        }

        if ($candidateFile.Name -eq "AssemblyInfo.cs") {
            continue
        }

        $fileLines = Read-FileLinesUtf8 -FilePath $candidateFile.FullName
        if (($fileLines -join "`n") -match "<auto-generated") {
            continue
        }

        [pscustomobject]@{
            FilePath = $candidateFile.FullName
            Lines    = $fileLines
        }
    }
}

function Get-CommentMetadata {
    param(
        [string[]]$Lines
    )

    $commentTexts = @{}
    $docCommentLines = New-Object System.Collections.Generic.HashSet[int]
    $blockCommentLines = New-Object System.Collections.Generic.HashSet[int]
    $singleCommentLines = New-Object System.Collections.Generic.HashSet[int]
    $allCommentLines = New-Object System.Collections.Generic.HashSet[int]

    $insideBlockComment = $false

    for ($lineIndex = 0; $lineIndex -lt $Lines.Length; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        $lineText = $Lines[$lineIndex]
        $trimmedLine = $lineText.Trim()

        if ($insideBlockComment) {
            $allCommentLines.Add($lineNumber) | Out-Null
            $blockCommentLines.Add($lineNumber) | Out-Null
            $commentTexts[$lineNumber] = $trimmedLine

            if ($trimmedLine -match "\*/") {
                $insideBlockComment = $false
            }

            continue
        }

        if ($trimmedLine.StartsWith("///", [System.StringComparison]::Ordinal)) {
            $allCommentLines.Add($lineNumber) | Out-Null
            $docCommentLines.Add($lineNumber) | Out-Null
            $commentTexts[$lineNumber] = $trimmedLine.Substring(3).Trim()
            continue
        }

        if ($trimmedLine.StartsWith("//", [System.StringComparison]::Ordinal)) {
            $allCommentLines.Add($lineNumber) | Out-Null
            $singleCommentLines.Add($lineNumber) | Out-Null
            $commentTexts[$lineNumber] = $trimmedLine.Substring(2).Trim()
            continue
        }

        if ($trimmedLine.StartsWith("/*", [System.StringComparison]::Ordinal)) {
            $allCommentLines.Add($lineNumber) | Out-Null
            $blockCommentLines.Add($lineNumber) | Out-Null
            $commentTexts[$lineNumber] = $trimmedLine

            if ($trimmedLine -notmatch "\*/") {
                $insideBlockComment = $true
            }
        }
    }

    return [pscustomobject]@{
        CommentTexts       = $commentTexts
        AllCommentLines    = $allCommentLines
        DocCommentLines    = $docCommentLines
        BlockCommentLines  = $blockCommentLines
        SingleCommentLines = $singleCommentLines
    }
}

function Test-CommentBlockAboveDeclaration {
    param(
        [string[]]$Lines,
        [int]$DeclarationLineNumber,
        [pscustomobject]$CommentMetadata
    )

    $scanIndex = $DeclarationLineNumber - 2

    while ($scanIndex -ge 0) {
        $trimmedLine = $Lines[$scanIndex].Trim()

        if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
            return $false
        }

        $candidateLineNumber = $scanIndex + 1
        if ($CommentMetadata.AllCommentLines.Contains($candidateLineNumber)) {
            break
        }

        if ($trimmedLine.EndsWith("]", [System.StringComparison]::Ordinal)) {
            while ($scanIndex -ge 0) {
                $attributeLine = $Lines[$scanIndex].Trim()
                if ($attributeLine.StartsWith("[", [System.StringComparison]::Ordinal)) {
                    $scanIndex--
                    break
                }

                $scanIndex--
            }

            continue
        }

        if ($trimmedLine.StartsWith("[", [System.StringComparison]::Ordinal)) {
            $scanIndex--
            continue
        }

        break
    }

    if ($scanIndex -lt 0) {
        return $false
    }

    $candidateLineNumber = $scanIndex + 1
    if ($CommentMetadata.AllCommentLines.Contains($candidateLineNumber)) {
        return $true
    }

    return $false
}

function Test-MethodDeclaration {
    param(
        [string]$LineText
    )

    $trimmedLine = $LineText.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
        return $false
    }

    if ($trimmedLine.StartsWith("//", [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmedLine.StartsWith("/*", [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmedLine -cmatch "^(if|for|foreach|while|switch|catch|lock|using)\s*\(" -or $trimmedLine -cmatch "^(return|throw|await|var|new|this|base)\b") {
        return $false
    }

    if ($trimmedLine -notmatch "\(" -or $trimmedLine -notmatch "\)") {
        return $false
    }

    if ($trimmedLine -cmatch "\b(delegate|record|class|interface|struct|enum)\b") {
        return $false
    }

    $hasAccessModifier = $trimmedLine -cmatch "^(public|protected|internal|private|file|static|virtual|override|abstract|sealed|async|extern|unsafe|new)\b"
    $isInterfaceStyleSignature = $trimmedLine -match "^[A-Za-z_][\w<>\[\],?\s]*\s+[A-Za-z_]\w*\s*\([^=]*\)\s*;$"

    if (-not ($hasAccessModifier -or $isInterfaceStyleSignature)) {
        return $false
    }

    if ($trimmedLine -notmatch "\)\s*(\{|=>|;)" -and $trimmedLine -notmatch "\)\s*where\s" -and -not $trimmedLine.EndsWith("(", [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmedLine -match "=\s*[^;]*\(") {
        return $false
    }

    return $true
}

function Test-PropertyDeclaration {
    param(
        [string]$LineText
    )

    $trimmedLine = $LineText.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
        return $false
    }

    if ($trimmedLine.StartsWith("//", [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmedLine.StartsWith("/*", [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmedLine -match "\(") {
        return $false
    }

    if (-not ($trimmedLine -cmatch "^(public|protected|internal|private|file|static|virtual|override|abstract|sealed|required|new)\b")) {
        return $false
    }

    if ($trimmedLine -match "\b(class|interface|enum|record|struct)\b") {
        return $false
    }

    if ($trimmedLine -match "\{[^\}]*\b(get|set|init)\b" -or $trimmedLine -match "=>") {
        return $true
    }

    if ($trimmedLine.EndsWith(";", [System.StringComparison]::Ordinal) -or $trimmedLine -match "=") {
        return $false
    }

    $propertySignatureText = [regex]::Replace($trimmedLine, "^(public|protected|internal|private|file|static|virtual|override|abstract|sealed|required|new)\s+", [string]::Empty)
    $propertyNameMatches = [regex]::Matches($propertySignatureText, "\b[A-Za-z_][A-Za-z0-9_]*\b")
    if ($propertyNameMatches.Count -ge 2 -and $trimmedLine -cmatch "\s+[A-Za-z_][A-Za-z0-9_]*\s*$") {
        return $true
    }

    return $false
}

function Test-FieldDeclaration {
    param(
        [string]$LineText
    )

    $trimmedLine = $LineText.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
        return $false
    }

    if ($trimmedLine.StartsWith("//", [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmedLine.StartsWith("/*", [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmedLine -match "=>") {
        return $false
    }

    $openParenthesisIndex = $trimmedLine.IndexOf("(", [System.StringComparison]::Ordinal)
    $equalsIndex = $trimmedLine.IndexOf("=", [System.StringComparison]::Ordinal)
    if ($openParenthesisIndex -ge 0 -and ($equalsIndex -lt 0 -or $openParenthesisIndex -lt $equalsIndex)) {
        return $false
    }

    if (-not ($trimmedLine -cmatch "^(public|protected|internal|private|file|static|readonly|volatile|required|const)\b")) {
        return $false
    }

    if ($trimmedLine -match "\b(class|interface|enum|record|struct|namespace|using)\b") {
        return $false
    }

    if ($trimmedLine.EndsWith(";") -and $trimmedLine -notmatch "\{") {
        return $true
    }

    return $false
}

function Get-MethodEndLineNumber {
    param(
        [string[]]$Lines,
        [int]$StartLineNumber
    )

    $braceDepth = 0
    $opened = $false

    for ($lineIndex = $StartLineNumber - 1; $lineIndex -lt $Lines.Length; $lineIndex++) {
        $lineText = $Lines[$lineIndex]
        $openCount = ([regex]::Matches($lineText, "\{")).Count
        $closeCount = ([regex]::Matches($lineText, "\}")).Count

        if ($openCount -gt 0) {
            $opened = $true
        }

        $braceDepth += $openCount
        $braceDepth -= $closeCount

        if ($opened -and $braceDepth -le 0) {
            return $lineIndex + 1
        }
    }

    return $Lines.Length
}

function Test-ComplexMethodNeedsStepComment {
    param(
        [string[]]$Lines,
        [int]$MethodStartLineNumber,
        [int]$MethodEndLineNumber,
        [pscustomobject]$CommentMetadata
    )

    if ($Lines[$MethodStartLineNumber - 1].Trim() -match "=>|;\s*$") {
        return [pscustomobject]@{
            IsComplexMethod = $false
            HasInnerComment = $false
        }
    }

    $nonEmptyLineCount = 0
    $controlFlowCount = 0
    $hasInnerComment = $false

    for ($lineIndex = $MethodStartLineNumber; $lineIndex -lt $MethodEndLineNumber - 1; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        $trimmedLine = $Lines[$lineIndex].Trim()

        if (-not [string]::IsNullOrWhiteSpace($trimmedLine)) {
            $nonEmptyLineCount++
        }

        if ($trimmedLine -match "\b(if|for|foreach|while|switch|try|catch)\b") {
            $controlFlowCount++
        }

        if ($CommentMetadata.AllCommentLines.Contains($lineNumber)) {
            $hasInnerComment = $true
        }
    }

    $isComplexMethod = $nonEmptyLineCount -ge 12 -or $controlFlowCount -ge 2

    return [pscustomobject]@{
        IsComplexMethod = $isComplexMethod
        HasInnerComment = $hasInnerComment
    }
}

function Test-UtcUsage {
    param(
        [string]$FilePath,
        [string[]]$Lines
    )

    for ($lineIndex = 0; $lineIndex -lt $Lines.Length; $lineIndex++) {
        $lineText = $Lines[$lineIndex]

        foreach ($utcPattern in $script:UtcPatterns) {
            if ($lineText -match $utcPattern) {
                Add-Violation -FilePath $FilePath -LineNumber ($lineIndex + 1) -Message "UTC time usage is forbidden."
                break
            }
        }
    }
}

function Test-EnvironmentVariableUsage {
    param(
        [string]$FilePath,
        [string[]]$Lines,
        [pscustomobject]$CommentMetadata
    )

    for ($lineIndex = 0; $lineIndex -lt $Lines.Length; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        if ($CommentMetadata.AllCommentLines.Contains($lineNumber)) {
            continue
        }

        $lineText = $Lines[$lineIndex]
        foreach ($environmentVariablePattern in $script:EnvironmentVariablePatterns) {
            if ($lineText -match $environmentVariablePattern) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Environment variable usage is forbidden. Use appsettings, User Secrets, or command-line arguments instead."
                break
            }
        }
    }
}

function Test-ThreadPoolStarvationRisk {
    param(
        [string]$FilePath,
        [string[]]$Lines,
        [pscustomobject]$CommentMetadata
    )

    for ($lineIndex = 0; $lineIndex -lt $Lines.Length; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        if ($CommentMetadata.AllCommentLines.Contains($lineNumber)) {
            continue
        }

        $lineText = $Lines[$lineIndex]
        foreach ($threadPoolStarvationRiskPattern in $script:ThreadPoolStarvationRiskPatterns) {
            if ($lineText -match $threadPoolStarvationRiskPattern) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Task.Run is forbidden by the compile guard because it can hide ThreadPool starvation risk. Use BackgroundService, async pipelines, channels, or a bounded queue instead."
                break
            }
        }
    }
}

function Test-FixedDecimalRules {
    param(
        [string]$FilePath,
        [string[]]$Lines,
        [pscustomobject]$CommentMetadata
    )

    for ($lineIndex = 0; $lineIndex -lt $Lines.Length; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        if ($CommentMetadata.AllCommentLines.Contains($lineNumber)) {
            continue
        }

        $lineText = $Lines[$lineIndex]

        foreach ($floatingPointPattern in $script:FloatingPointPatterns) {
            if ($lineText -match $floatingPointPattern) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Floating-point types are forbidden. Use decimal with at most 3 fractional digits."
                break
            }
        }

        if ($lineText -match '\.TotalMilliseconds\b' -or $lineText -match '\.TotalSeconds\b' -or $lineText -match '\.TotalMinutes\b') {
            Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "TimeSpan floating-point accessors are forbidden. Convert from ticks to decimal instead."
        }

        if ($lineText -match 'Math\.Round\s*\(.*?,\s*(\d+)\s*,\s*MidpointRounding') {
            $roundScale = [int]$Matches[1]
            if ($roundScale -ne 3) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Decimal rounding scale must be exactly 3."
            }
        }

        $storeTypeMatches = [regex]::Matches($lineText, '(decimal|numeric)\s*\(\s*\d+\s*,\s*(\d+)\s*\)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        foreach ($storeTypeMatch in $storeTypeMatches) {
            $storeScale = [int]$storeTypeMatch.Groups[2].Value
            if ($storeScale -ne 3) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Decimal database precision must use scale 3."
            }
        }

        $precisionMatches = [regex]::Matches($lineText, 'HasPrecision\s*\(\s*\d+\s*,\s*(\d+)\s*\)')
        foreach ($precisionMatch in $precisionMatches) {
            $precisionScale = [int]$precisionMatch.Groups[1].Value
            if ($precisionScale -ne 3) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Entity decimal precision must use scale 3."
            }
        }

        $decimalLiteralMatches = [regex]::Matches($lineText, '\b\d+\.(\d+)M\b', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        foreach ($decimalLiteralMatch in $decimalLiteralMatches) {
            $fractionalDigits = $decimalLiteralMatch.Groups[1].Value.Length
            if ($fractionalDigits -gt 3) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Decimal literals cannot exceed 3 fractional digits."
            }
        }
    }
}

function Test-EnumMemberConventions {
    param(
        [string]$FilePath,
        [string[]]$Lines
    )

    $insideEnum = $false
    $enumBraceDepth = 0
    $enumPending = $false
    $memberMetadataLines = New-Object System.Collections.Generic.List[int]

    for ($lineIndex = 0; $lineIndex -lt $Lines.Length; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        $lineText = $Lines[$lineIndex]
        $trimmedLine = $lineText.Trim()

        if (-not $insideEnum) {
            if ($trimmedLine -match '\benum\s+[A-Za-z_][A-Za-z0-9_]*\b') {
                $enumPending = $true
                $enumBraceDepth = 0
                $memberMetadataLines.Clear()
            }

            if ($enumPending) {
                $enumBraceDepth += ([regex]::Matches($lineText, "\{")).Count
                $enumBraceDepth -= ([regex]::Matches($lineText, "\}")).Count
                if ($enumBraceDepth -gt 0) {
                    $insideEnum = $true
                    $enumPending = $false
                }
            }

            continue
        }

        $openCount = ([regex]::Matches($lineText, "\{")).Count
        $closeCount = ([regex]::Matches($lineText, "\}")).Count
        $enumBraceDepth += $openCount
        $enumBraceDepth -= $closeCount

        if ($enumBraceDepth -le 0) {
            $insideEnum = $false
            $enumPending = $false
            $memberMetadataLines.Clear()
            continue
        }

        if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
            continue
        }

        if ($trimmedLine.StartsWith("///", [System.StringComparison]::Ordinal) -or
            $trimmedLine.StartsWith("[", [System.StringComparison]::Ordinal)) {
            $memberMetadataLines.Add($lineIndex) | Out-Null
            continue
        }

        if ($trimmedLine.StartsWith("//", [System.StringComparison]::Ordinal) -or
            $trimmedLine.StartsWith("/*", [System.StringComparison]::Ordinal) -or
            $trimmedLine.StartsWith("*", [System.StringComparison]::Ordinal)) {
            continue
        }

        $memberMatch = [regex]::Match($trimmedLine, $script:EnumMemberLinePattern)
        if (-not $memberMatch.Success) {
            $memberMetadataLines.Clear()
            continue
        }

        $metadataTexts = New-Object System.Collections.Generic.List[string]
        foreach ($metadataLineIndex in $memberMetadataLines) {
            $metadataTexts.Add($Lines[$metadataLineIndex].Trim()) | Out-Null
        }

        $docTexts = New-Object System.Collections.Generic.List[string]
        $descriptionTexts = New-Object System.Collections.Generic.List[string]
        foreach ($metadataText in $metadataTexts) {
            if ($metadataText.StartsWith("///", [System.StringComparison]::Ordinal)) {
                $docTexts.Add((Get-DocCommentPlainText -Text $metadataText.Substring(3).Trim())) | Out-Null
                continue
            }

            $descriptionMatch = [regex]::Match($metadataText, '^\[Description(?:Attribute)?\s*\(\s*"([^"]+)"\s*\)\]$')
            if ($descriptionMatch.Success) {
                $descriptionTexts.Add($descriptionMatch.Groups[1].Value) | Out-Null
            }
        }

        if ($docTexts.Count -eq 0) {
            Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Enum members must have Chinese XML comments above the declaration."
        }
        else {
            $docText = (($docTexts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join " ").Trim()
            if ([string]::IsNullOrWhiteSpace($docText) -or -not (Test-ChineseText -Text $docText)) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Enum member XML comments must use Chinese."
            }

            if (Test-GarbledText -Text $docText) {
                Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Enum member XML comments contain garbled text."
            }
        }

        if ($descriptionTexts.Count -eq 0) {
            Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Enum members must have a Description attribute."
        }
        else {
            foreach ($descriptionText in $descriptionTexts) {
                if (-not (Test-ChineseText -Text $descriptionText)) {
                    Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Enum member Description values must use Chinese."
                }

                if (Test-GarbledText -Text $descriptionText) {
                    Add-Violation -FilePath $FilePath -LineNumber $lineNumber -Message "Enum member Description values contain garbled text."
                }
            }
        }

        $memberMetadataLines.Clear()
    }
}

$sourceFiles = @(Get-SourceFiles -RootDirectory $ProjectDirectory)

foreach ($sourceFile in $sourceFiles) {
    $filePath = $sourceFile.FilePath
    $lines = $sourceFile.Lines
    $commentMetadata = Get-CommentMetadata -Lines $lines

    foreach ($commentEntry in $commentMetadata.CommentTexts.GetEnumerator()) {
        $commentLineNumber = [int]$commentEntry.Key
        $commentText = [string]$commentEntry.Value
        $commentValidationText = $commentText

        if ($commentMetadata.DocCommentLines.Contains($commentLineNumber)) {
            $commentValidationText = Get-DocCommentPlainText -Text $commentText

            if ([string]::IsNullOrWhiteSpace($commentValidationText)) {
                continue
            }
        }

        if (-not (Test-ChineseText -Text $commentValidationText)) {
            Add-Violation -FilePath $filePath -LineNumber $commentLineNumber -Message "Comments must use Chinese."
        }

        if (Test-GarbledText -Text $commentValidationText) {
            Add-Violation -FilePath $filePath -LineNumber $commentLineNumber -Message "Comments contain garbled text."
        }

        if (Test-PlaceholderComment -Text $commentValidationText) {
            Add-Violation -FilePath $filePath -LineNumber $commentLineNumber -Message "Comments must be meaningful and cannot use placeholder text."
        }

        if (Test-GenericNameOnlyComment -FilePath $filePath -Text $commentValidationText) {
            Add-Violation -FilePath $filePath -LineNumber $commentLineNumber -Message "Swagger contract comments must describe business meaning and cannot only repeat type or property names."
        }
    }

    Test-UtcUsage -FilePath $filePath -Lines $lines
    Test-EnvironmentVariableUsage -FilePath $filePath -Lines $lines -CommentMetadata $commentMetadata
    Test-ThreadPoolStarvationRisk -FilePath $filePath -Lines $lines -CommentMetadata $commentMetadata
    Test-FixedDecimalRules -FilePath $filePath -Lines $lines -CommentMetadata $commentMetadata
    Test-EnumMemberConventions -FilePath $filePath -Lines $lines

    for ($lineIndex = 0; $lineIndex -lt $lines.Length; $lineIndex++) {
        $lineNumber = $lineIndex + 1
        $lineText = $lines[$lineIndex]

        if (Test-ForbiddenUnicodeUsage -Text $lineText) {
            Add-Violation -FilePath $filePath -LineNumber $lineNumber -Message "Unicode escape sequences and invisible Unicode characters are forbidden."
        }

        if (Test-GarbledText -Text $lineText) {
            Add-Violation -FilePath $filePath -LineNumber $lineNumber -Message "Source text contains garbled Chinese."
        }

        if (Test-MethodDeclaration -LineText $lineText) {
            if (-not (Test-CommentBlockAboveDeclaration -Lines $lines -DeclarationLineNumber $lineNumber -CommentMetadata $commentMetadata)) {
                Add-Violation -FilePath $filePath -LineNumber $lineNumber -Message "Methods must have Chinese comments above the declaration."
            }

            $methodEndLineNumber = Get-MethodEndLineNumber -Lines $lines -StartLineNumber $lineNumber
            $complexMethodResult = Test-ComplexMethodNeedsStepComment -Lines $lines -MethodStartLineNumber $lineNumber -MethodEndLineNumber $methodEndLineNumber -CommentMetadata $commentMetadata

            if ($complexMethodResult.IsComplexMethod -and -not $complexMethodResult.HasInnerComment) {
                Add-Violation -FilePath $filePath -LineNumber $lineNumber -Message "Complex methods must contain step comments in Chinese."
            }

            continue
        }

        if (Test-PropertyDeclaration -LineText $lineText) {
            if (-not (Test-CommentBlockAboveDeclaration -Lines $lines -DeclarationLineNumber $lineNumber -CommentMetadata $commentMetadata)) {
                Add-Violation -FilePath $filePath -LineNumber $lineNumber -Message "Properties must have Chinese comments above the declaration."
            }

            continue
        }

        if (Test-FieldDeclaration -LineText $lineText) {
            if (-not (Test-CommentBlockAboveDeclaration -Lines $lines -DeclarationLineNumber $lineNumber -CommentMetadata $commentMetadata)) {
                Add-Violation -FilePath $filePath -LineNumber $lineNumber -Message "Fields must have Chinese comments above the declaration."
            }
        }
    }
}

if ($script:ViolationMessages.Count -gt 0) {
    [Console]::Error.WriteLine("Code convention validation failed for project '$ProjectName'.")

    $displayCount = [Math]::Min($script:ViolationMessages.Count, $script:ViolationLimit)
    for ($index = 0; $index -lt $displayCount; $index++) {
        [Console]::Error.WriteLine($script:ViolationMessages[$index])
    }

    if ($script:ViolationMessages.Count -gt $script:ViolationLimit) {
        [Console]::Error.WriteLine("... and {0} more violations." -f ($script:ViolationMessages.Count - $script:ViolationLimit))
    }

    exit 1
}

[Console]::WriteLine("Code convention validation passed for project '{0}'." -f $ProjectName)
