<#
.SYNOPSIS
    Updates the packaged app identity metadata in the manifest to match the Store
    or sideloaded app configuration.

.DESCRIPTION
    The Update-AppxManifest.ps1 script updates the package identity name and publisher
    values in Package.appxmanifest, and ensures the Windows app display name uses
    the localized manifest resource entry.

.PARAMETER Unsigned
    Indicates that Update-AppxManifest updates the app manifest for sideloaded
    deployments by using the unsigned publisher value.

.EXAMPLE
    PS> ./Update-AppxManifest.ps1

    Updates the manifest using the default signed Store identity and localized app
    display name.

.EXAMPLE
    PS> ./Update-AppxManifest.ps1 -Unsigned

    Updates the manifest using the unsigned publisher value for sideloaded
    deployments.

.NOTES
    The script is intended for UWP projects.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [switch]$Unsigned
)

New-Variable -Name IdentityName -Value "18496Starpine.Screenbox" -Option Constant
New-Variable -Name IdentityPublisher -Value 'CN=ABCDF790-DBE4-48F7-8204-32FCB69ADF9C' -Option Constant
New-Variable -Name IdentityPublisherUnsigned -Value "$IdentityPublisher, OID.2.25.311729368913984317654407730594956997722=1" -Option Constant

New-Variable -Name DisplayName -Value "Screenbox" -Option Constant
New-Variable -Name VisualDisplayName -Value "ms-resource:ManifestResources/AppDisplayName" -Option Constant

$repositoryPath = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path -Path $repositoryPath -ChildPath "Screenbox/Package.appxmanifest"

$publisher = if (!$Unsigned) { $IdentityPublisher } else { $IdentityPublisherUnsigned }

[xml]$xmlDoc = Get-Content -Path $manifestPath -Raw

$xmlDoc.Package.Identity.Name = $IdentityName
$xmlDoc.Package.Identity.Publisher = $publisher

$xmlDoc.Package.Properties.DisplayName = $DisplayName
$xmlDoc.Package.Applications.Application.VisualElements.DisplayName = $VisualDisplayName

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$settings.Indent = $true
#$settings.NewLineChars = "`r`n"

$writer = [System.Xml.XmlWriter]::Create($manifestPath, $settings)

try {
    $xmlDoc.Save($writer)
}
finally {
    $writer.Dispose()
}
