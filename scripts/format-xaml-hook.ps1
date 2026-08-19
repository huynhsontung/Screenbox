param()

$ErrorActionPreference = 'SilentlyContinue'

try {
    # Resolve repository root
    $repoRoot = git rev-parse --show-toplevel 2>$null
    if (-not $repoRoot) {
        $repoRoot = Split-Path -Parent $PSScriptRoot
    }

    # Find modified uncommitted XAML files (excluding obj/bin)
    $modifiedXaml = git -C $repoRoot diff --name-only | Where-Object { 
        $_ -like '*.xaml' -and 
        $_ -notmatch "(\\obj\\)|(\\bin\\)" -and 
        (Test-Path (Join-Path $repoRoot $_)) 
    } | ForEach-Object { Join-Path $repoRoot $_ }

    if ($modifiedXaml -and $modifiedXaml.Count -gt 0) {
        dotnet tool run xstyler -f $modifiedXaml 2>&1 | Out-Null
    }
} catch {
    # Fail silently to avoid blocking agent execution loop
}

# PostToolUse contract requires a JSON object output on stdout
Write-Output "{}"
