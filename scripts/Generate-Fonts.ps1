$repositoryPath = Split-Path -Parent $PSScriptRoot
$fontsPath = Join-Path -Path $repositoryPath -ChildPath "assets\fonts"
$scriptPath = Join-Path -Path $repositoryPath -ChildPath "assets\fonts\fontforge-ufo-to-ttf.pe"

# Check if FontForge is installed.
if (-not (Get-Command "fontforge" -ErrorAction SilentlyContinue)) {
    Write-Error "FontForge is not installed or not in PATH environment variable."
    exit 1
}

$ufoDirs = @(Get-ChildItem -Path $fontsPath -Filter '*.ufo' -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)

if ($ufoDirs.Count -gt 0) {
    Write-Host "Found UFO packages:`n$($ufoDirs -join "`n")"

    # Build argument list: -script <scriptFile> <ufo#1> <ufo#2> ...
    $cmdArgs = @('-script', $scriptPath) + $ufoDirs
    & "fontforge" @cmdArgs
} else {
    Write-Host "No UFO packages found in the folder $fontsPath"
    exit 0
}
