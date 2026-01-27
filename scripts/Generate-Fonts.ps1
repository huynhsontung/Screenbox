<#
.SYNOPSIS
    Generate TrueType (.ttf) fonts from UFO packages using FontForge.
.DESCRIPTION
    Lightweight wrapper that invokes `assets/fonts/fontforge-ufo-to-ttf.pe`
    to generate .ttf files.
.PARAMETER Publish
    (Optional) Specifies whether generated .ttf files should be moved from the
    source assets folder into the application assets folder. If not specified,
    generated .ttf files remain in the source assets folder.
.EXAMPLE
    PS> .\scripts\Generate-Fonts.ps1
.EXAMPLE
    PS> .\scripts\Generate-Fonts.ps1 -Publish
.NOTES
    FontForge must be installed and included on the system PATH environment
    variables.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [switch]
    $Publish
)

New-Variable -Name 'ScriptFileName' -Value 'fontforge-ufo-to-ttf.pe' -Option Constant

$repositoryPath = Split-Path -Parent $PSScriptRoot
$fontsPath = Join-Path -Path $repositoryPath -ChildPath "assets\fonts"
$publishPath = Join-Path -Path $repositoryPath -ChildPath "Screenbox\Assets\Fonts"

if (-not (Get-Command "fontforge" -ErrorAction SilentlyContinue)) {
    Write-Error "FontForge is not installed or not in PATH environment variable."
    exit 1
}

$ufoDirs = @(Get-ChildItem -Path $fontsPath -Filter '*.ufo' -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)

if ($ufoDirs.Count -gt 0) {
    Write-Host "Found UFO packages:`n$($ufoDirs -join "`n")"

    # Build argument list: -script <scriptFile> <ufo#1> <ufo#2> ...
    $cmdArgs = @('-script', (Join-Path -Path $fontsPath -ChildPath $ScriptFileName)) + $ufoDirs
    & "fontforge" @cmdArgs

    if ($Publish) {
        try {
            Move-Item -Path (Join-Path $fontsPath '*.ttf') -Destination $publishPath -Force -ErrorAction Stop
        } catch [System.Management.Automation.ItemNotFoundException] {
            Write-Host "No .ttf files found in the folder $fontsPath."
        } catch {
            Write-Error $_.Exception.Message
            exit 1
        }
    }

} else {
    Write-Host "No UFO packages found in the folder $fontsPath."
    exit 0
}
