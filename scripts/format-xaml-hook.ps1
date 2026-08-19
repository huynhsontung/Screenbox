param()

$ErrorActionPreference = 'SilentlyContinue'

try {
    $inputJson = $null

    # Consume piped JSON payload from Antigravity hook runner
    $rawInput = ($input | Out-String).Trim()
    if (-not [string]::IsNullOrWhiteSpace($rawInput)) {
        $inputJson = $rawInput | ConvertFrom-Json
    }

    # Extract TargetFile from toolCall arguments if present
    $targetFile = $null
    if ($inputJson -and $inputJson.toolCall -and $inputJson.toolCall.args) {
        if ($inputJson.toolCall.args.TargetFile) {
            $targetFile = $inputJson.toolCall.args.TargetFile
        }
    }

    if ($targetFile) {
        # Fast exit if the modified file is not a XAML file
        if ($targetFile -notlike '*.xaml') {
            Write-Output "{}"
            exit 0
        }

        # Resolve full path if relative
        $filePath = if (Test-Path $targetFile) { $targetFile } else { Join-Path (Split-Path -Parent $PSScriptRoot) $targetFile }

        # Format only the targeted XAML file if it exists
        if (Test-Path $filePath) {
            dotnet tool run xstyler -f $filePath 2>&1 | Out-Null
        }
    }
    else {
        # Fallback if no specific TargetFile in payload: format modified XAML files from git diff
        $repoRoot = git rev-parse --show-toplevel 2>$null
        if (-not $repoRoot) {
            $repoRoot = Split-Path -Parent $PSScriptRoot
        }

        $modifiedXaml = git -C $repoRoot diff --name-only | Where-Object { 
            $_ -like '*.xaml' -and 
            $_ -notmatch "(\\obj\\)|(\\bin\\)" -and 
            (Test-Path (Join-Path $repoRoot $_)) 
        } | ForEach-Object { Join-Path $repoRoot $_ }

        if ($modifiedXaml -and $modifiedXaml.Count -gt 0) {
            dotnet tool run xstyler -f $modifiedXaml 2>&1 | Out-Null
        }
    }
} catch {
    # Fail silently to avoid blocking the agent execution loop
}

# PostToolUse contract requires a JSON object on stdout
Write-Output "{}"
