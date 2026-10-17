<#
.SYNOPSIS
    Saves the latest Screenbox sideload output into a install kit.

.DESCRIPTION
    The Export-ScreenboxInstallKit.ps1 script finds the newest package, copies the
    required runtime dependencies, optionally adds the signing certificate and/or
    MSIX bundle, and creates a ZIP archive for easier distribution.

.PARAMETER OutputPath
    This parameter is required and specifies the path to the archive file. The OutputPath
    should include the name and either the absolute or relative path.

    If the file name in OutputPath doesn't have a .zip file name extension, one will
    be added automatically.

.PARAMETER IncludeBundle
    Includes the platform bundle (.msixbundle) in the output archive.

.PARAMETER IncludeCertificate
    Includes the signing certificate (.cer) file in the output archive.

.EXAMPLE
    PS> ./scripts/Export-ScreenboxInstallKit.ps1 -OutputPath InstallKit.zip

    Exports the install kit with only the dependencies to a archive named InstallKit.zip
    in the current directory.

.EXAMPLE
    PS> ./scripts/Export-ScreenboxInstallKit.ps1 -OutputPath "C:\Archives\InstallKit.zip" -IncludeBundle

    Includes the MSIX bundle in the archive to the specified directory.

.EXAMPLE
    PS> ./scripts/Export-ScreenboxInstallKit.ps1 -OutputPath "C:\Archives\InstallKit.zip" -IncludeCertificate

    Includes the signing certificate in the archive to the specified directory.

.NOTES
    The script is intended to facilitate the creation of a Screenbox install kit.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath,

    [Parameter(Mandatory = $false)]
    [switch]$IncludeBundle,

    [Parameter(Mandatory = $false)]
    [switch]$IncludeCertificate
)

$repositoryPath = Split-Path -Parent $PSScriptRoot
$packagePath = Get-ChildItem -Path (Join-Path $repositoryPath -ChildPath 'Screenbox/AppPackages') -Directory -Filter '*_Test' |
    Sort-Object -Property LastWriteTime -Descending |
    Select-Object -ExpandProperty FullName -First 1

if (-not $packagePath) {
    throw "No package folder could be found in $(Join-Path $repositoryPath 'Screenbox/AppPackages')."
}

$dependenciesPath = Join-Path -Path $packagePath -ChildPath 'Dependencies'

# Create temporary folder
$tempPath = Join-Path $packagePath -ChildPath 'Screenbox_PreInstallKit_Temp'

Remove-Item $tempPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $tempPath -Force | Out-Null

# Create temporary Dependencies folder
$tempDependenciesPath = Join-Path -Path $tempPath -ChildPath 'Dependencies'

New-Item -ItemType Directory -Path $tempDependenciesPath -Force | Out-Null

# Copy architecture-specific dependencies
foreach ($processorArchitecture in @('ARM64', 'x64', 'x86')) {
    $architecturePath = Join-Path -Path $dependenciesPath -ChildPath $processorArchitecture
    if (Test-Path $architecturePath) {
        Copy-Item -Path $architecturePath -Destination $tempDependenciesPath -Recurse -Force
    }
}

# Copy certificate (if requested)
if ($IncludeCertificate) {
    $certificatePath = Get-ChildItem -Path $packagePath -File -Filter *.cer |
        Select-Object -ExpandProperty FullName -First 1

    if ($certificatePath -and (Test-Path -LiteralPath $certificatePath)) {
        Copy-Item -Path $certificatePath -Destination $tempPath -Force
    }
    else {
        Write-Warning "No certificate (.cer) file found in the folder: $packagePath."
    }
}

# Copy bundle (if requested)
if ($IncludeBundle) {
    $bundlePath = Get-ChildItem -Path $packagePath -File -Filter *.msixbundle |
        Select-Object -ExpandProperty FullName -First 1

    if ($bundlePath -and (Test-Path -LiteralPath $bundlePath)) {
        Copy-Item -Path $bundlePath -Destination $tempPath -Force
    }
    else {
        Write-Warning "No package bundle (.msixbundle) file found in the folder: $packagePath."
    }
}

# Create ZIP archive
Compress-Archive -Path (Join-Path -Path $tempPath -ChildPath '*') `
    -DestinationPath $OutputPath `
    -CompressionLevel NoCompression `
    -Force

# Delete temporary folder
Remove-Item $tempPath -Recurse -Force
