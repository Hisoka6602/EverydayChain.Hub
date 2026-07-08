param(
    [string]$RootDirectory = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

function Read-FileLinesUtf8 {
    param(
        [string]$FilePath
    )

    return [System.IO.File]::ReadAllLines($FilePath, [System.Text.Encoding]::UTF8)
}

function Write-FileLinesUtf8 {
    param(
        [string]$FilePath,
        [string[]]$Lines
    )

    [System.IO.File]::WriteAllLines($FilePath, $Lines, [System.Text.Encoding]::UTF8)
}

function Get-SourceFiles {
    param(
        [string]$RootPath
    )

    $candidateFiles = Get-ChildItem -Path $RootPath -Recurse -File -Filter "*.cs"
    foreach ($candidateFile in $candidateFiles) {
        if ($candidateFile.FullName -match "[\\/](bin|obj)[\\/]") {
            continue
        }

        if ($candidateFile.FullName -match "[\\/]build[\\/](repair-test|repair-test2)[\\/]") {
            continue
        }

        if ($candidateFile.Name -match "(\.Designer|\.g|\.g\.i)\.cs$") {
            continue
        }

        $candidateFile.FullName
    }
}

function Test-MethodDeclaration {
    param(
        [string]$LineText
    )

    $trimmedLine = $LineText.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
        return $false
    }

    if ($trimmedLine.StartsWith("//", [System.StringComparison]::Ordinal) -or $trimmedLine.StartsWith("/*", [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($trimmedLine -cmatch "^(if|for|foreach|while|switch|catch|lock|using|return|throw|await|var|new|this|base)\b") {
        return $false
    }

    if ($trimmedLine -cmatch "\b(delegate|record|class|interface|struct|enum)\b") {
        return $false
    }

    return $trimmedLine -match "\(" -and $trimmedLine -match "\)"
}

function Get-DeclarationContext {
    param(
        [string[]]$Lines,
        [int]$StartIndex
    )

    $parts = New-Object System.Collections.Generic.List[string]
    for ($index = $StartIndex; $index -lt $Lines.Length -and $index -lt ($StartIndex + 6); $index++) {
        $trimmedLine = $Lines[$index].Trim()
        if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
            continue
        }

        if ($trimmedLine.StartsWith("[", [System.StringComparison]::Ordinal)) {
            continue
        }

        $parts.Add($trimmedLine)
        if ($trimmedLine.Contains("{") -or $trimmedLine.Contains("=>") -or $trimmedLine.EndsWith(";")) {
            break
        }
    }

    return ($parts -join " ").Trim()
}

function Get-NextDeclarationContext {
    param(
        [string[]]$Lines,
        [int]$LineIndex
    )

    for ($index = $LineIndex + 1; $index -lt $Lines.Length; $index++) {
        $trimmedLine = $Lines[$index].Trim()
        if ([string]::IsNullOrWhiteSpace($trimmedLine)) {
            continue
        }

        if ($trimmedLine.StartsWith("///", [System.StringComparison]::Ordinal) -or $trimmedLine.StartsWith("//", [System.StringComparison]::Ordinal)) {
            continue
        }

        return Get-DeclarationContext -Lines $Lines -StartIndex $index
    }

    return [string]::Empty
}

function Get-NearestMethodContext {
    param(
        [string[]]$Lines,
        [int]$LineIndex
    )

    for ($index = $LineIndex; $index -ge 0; $index--) {
        $candidateText = Get-DeclarationContext -Lines $Lines -StartIndex $index
        if ([string]::IsNullOrWhiteSpace($candidateText)) {
            continue
        }

        if (Test-MethodDeclaration -LineText $candidateText) {
            return $candidateText
        }
    }

    return [string]::Empty
}

function Get-TypeName {
    param(
        [string]$DeclarationText
    )

    $match = [regex]::Match($DeclarationText, '\b(class|interface|struct|enum)\s+([A-Za-z_][\w]*)')
    if ($match.Success) {
        return $match.Groups[2].Value
    }

    $recordMatch = [regex]::Match($DeclarationText, '\brecord(?:\s+(?:class|struct))?\s+([A-Za-z_][\w]*)')
    if ($recordMatch.Success) {
        return $recordMatch.Groups[1].Value
    }

    return [string]::Empty
}

function Get-MethodName {
    param(
        [string]$DeclarationText
    )

    $match = [regex]::Match($DeclarationText, '([A-Za-z_][\w]*)\s*\(')
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    return [string]::Empty
}

function Get-PropertyName {
    param(
        [string]$DeclarationText
    )

    $lambdaMatch = [regex]::Match($DeclarationText, '([A-Za-z_][\w]*)\s*=>')
    if ($lambdaMatch.Success) {
        return $lambdaMatch.Groups[1].Value
    }

    $propertyMatch = [regex]::Match($DeclarationText, '([A-Za-z_][\w]*)\s*\{\s*(get|set|init)\b')
    if ($propertyMatch.Success) {
        return $propertyMatch.Groups[1].Value
    }

    return [string]::Empty
}

function Get-FieldName {
    param(
        [string]$DeclarationText
    )

    $cleanText = $DeclarationText.Trim().TrimEnd(';')
    $cleanText = $cleanText -replace '\s*=\s*.+$', ''
    $match = [regex]::Match($cleanText, '([A-Za-z_][\w]*)$')
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    return [string]::Empty
}

function Get-ReplacementCommentText {
    param(
        [string]$PlaceholderText,
        [string]$DeclarationContext,
        [string]$MethodContext
    )

    $normalizedPlaceholderText = $PlaceholderText.Trim()

    switch -Regex ($normalizedPlaceholderText) {
        '^定义当前类型。?$' {
            $typeName = Get-TypeName -DeclarationText $DeclarationContext
            if ([string]::IsNullOrWhiteSpace($typeName)) {
                return '定义业务相关类型。'
            }

            return "定义 $typeName 类型。"
        }
        '^定义当前成员。?$' {
            $propertyName = Get-PropertyName -DeclarationText $DeclarationContext
            if (-not [string]::IsNullOrWhiteSpace($propertyName)) {
                return "定义 $propertyName 成员。"
            }

            $fieldName = Get-FieldName -DeclarationText $DeclarationContext
            if (-not [string]::IsNullOrWhiteSpace($fieldName)) {
                return "定义 $fieldName 成员。"
            }

            $methodName = Get-MethodName -DeclarationText $DeclarationContext
            if ([string]::IsNullOrWhiteSpace($methodName)) {
                return '定义业务相关成员。'
            }

            return "定义 $methodName 成员。"
        }
        '^执行当前方法。?$' {
            $methodName = Get-MethodName -DeclarationText $DeclarationContext
            if ([string]::IsNullOrWhiteSpace($methodName)) {
                return '执行当前业务方法。'
            }

            return "执行 $methodName 方法。"
        }
        '^存储当前字段值。?$' {
            $fieldName = Get-FieldName -DeclarationText $DeclarationContext
            if ([string]::IsNullOrWhiteSpace($fieldName)) {
                return '存储业务相关字段。'
            }

            return "存储 $fieldName 字段。"
        }
        '^获取或设置当前属性值。?$' {
            $propertyName = Get-PropertyName -DeclarationText $DeclarationContext
            if ([string]::IsNullOrWhiteSpace($propertyName)) {
                return '获取或设置业务相关属性。'
            }

            return "获取或设置 $propertyName。"
        }
        '^步骤：按既定流程执行当前方法逻辑。?$' {
            $methodName = Get-MethodName -DeclarationText $MethodContext
            if ([string]::IsNullOrWhiteSpace($methodName)) {
                return '步骤：执行当前业务方法的核心处理流程。'
            }

            return "步骤：执行 $methodName 方法的核心处理流程。"
        }
        default {
            return $PlaceholderText
        }
    }
}

function Repair-SourceFile {
    param(
        [string]$FilePath
    )

    $lines = Read-FileLinesUtf8 -FilePath $FilePath
    $updated = $false

    for ($index = 0; $index -lt $lines.Length; $index++) {
        $trimmedLine = $lines[$index].Trim()
        $prefix = $null
        $placeholderText = $null

        if ($trimmedLine.StartsWith('/// ', [System.StringComparison]::Ordinal)) {
            $prefix = $lines[$index].Substring(0, $lines[$index].IndexOf('///', [System.StringComparison]::Ordinal)) + '/// '
            $placeholderText = $trimmedLine.Substring(4).Trim()
        }
        elseif ($trimmedLine.StartsWith('// ', [System.StringComparison]::Ordinal)) {
            $prefix = $lines[$index].Substring(0, $lines[$index].IndexOf('//', [System.StringComparison]::Ordinal)) + '// '
            $placeholderText = $trimmedLine.Substring(3).Trim()
        }
        elseif ($trimmedLine.StartsWith('///', [System.StringComparison]::Ordinal)) {
            $prefix = $lines[$index].Substring(0, $lines[$index].IndexOf('///', [System.StringComparison]::Ordinal)) + '/// '
            $placeholderText = $trimmedLine.Substring(3).Trim()
        }
        elseif ($trimmedLine.StartsWith('//', [System.StringComparison]::Ordinal)) {
            $prefix = $lines[$index].Substring(0, $lines[$index].IndexOf('//', [System.StringComparison]::Ordinal)) + '// '
            $placeholderText = $trimmedLine.Substring(2).Trim()
        }

        if ([string]::IsNullOrWhiteSpace($placeholderText)) {
            continue
        }

        $declarationContext = Get-NextDeclarationContext -Lines $lines -LineIndex $index
        $methodContext = Get-NearestMethodContext -Lines $lines -LineIndex $index
        $replacementText = Get-ReplacementCommentText -PlaceholderText $placeholderText -DeclarationContext $declarationContext -MethodContext $methodContext
        if ($replacementText -ne $placeholderText) {
            $lines[$index] = "$prefix$replacementText"
            $updated = $true
        }
    }

    if ($updated) {
        Write-FileLinesUtf8 -FilePath $FilePath -Lines $lines
    }
}

$sourceFiles = @(Get-SourceFiles -RootPath $RootDirectory)
foreach ($sourceFile in $sourceFiles) {
    Repair-SourceFile -FilePath $sourceFile
}

