<#
.SYNOPSIS
    Generate TrueType (.ttf) fonts from UFO packages using FontForge.
.DESCRIPTION
    Lightweight wrapper that invokes `assets/fonts/fontforge-ufo-to-ttf.pe`
    to generate .ttf files.
.PARAMETER Publish
    Specifies whether generated .ttf files should be moved from the
    source assets folder into the application assets folder. If not specified,
    generated .ttf files remain in the source assets folder.
.EXAMPLE
    PS> .\scripts\Generate-Fonts.ps1
.EXAMPLE
    PS> .\scripts\Generate-Fonts.ps1 -Publish
.NOTES
    FontForge must be installed and included in the PATH environment variable.
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
    Write-Error "Install the latest version of FontForge.`nAfter FontForge is installed, make sure that the location of the 'fontforge.exe' is included in the PATH environment variable." -Category NotInstalled
    exit 1
}

if (-not (Test-Path (Join-Path -Path $fontsPath -ChildPath $ScriptFileName))) {
    Write-Error "The script file $ScriptFileName was not found." -Category ObjectNotFound
    exit 1
}

$ufoDirs = @(Get-ChildItem -Path $fontsPath -Filter '*.ufo' -Directory -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)

if ($ufoDirs.Count -gt 0) {
    Write-Output "Found UFO packages:`n$($ufoDirs -join "`n")"

    # Build argument list: -script <scriptFile> <ufo#1> <ufo#2> ...
    $cmdArgs = @('-script', (Join-Path -Path $fontsPath -ChildPath $ScriptFileName)) + $ufoDirs
    & "fontforge" @cmdArgs

    if ($Publish) {
        try {
            $ttfFiles = @(Get-ChildItem -Path $fontsPath -Filter '*.ttf' -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)

            if ($ttfFiles.Count -eq 0) {
                Write-Error "No .ttf files found in the folder: $fontsPath" -Category ObjectNotFound
                exit 1
            }

            Move-Item -Path $ttfFiles -Destination $publishPath -Force -ErrorAction Stop
        }
        catch {
            Write-Error $_.Exception.Message
            exit 1
        }
    }

    exit 0
}
else {
    Write-Error "No UFO packages found in the folder: $fontsPath" -Category ObjectNotFound
    exit 1
}
