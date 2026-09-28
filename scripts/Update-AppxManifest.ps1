<#
.SYNOPSIS
    Updates the packaged app identity metadata in the manifest to match the Store
    or sideloaded app configuration.

.DESCRIPTION
    The Update-AppxManifest.ps1 script updates the package identity name, publisher
    and display name in Package.appxmanifest to match the specified identity configuration.

.PARAMETER Identity
    Specifies the application package identity that the manifest should use.

.EXAMPLE
    PS> ./Update-AppxManifest.ps1

    Updates the manifest using the default Store identity and localized app display name.

.EXAMPLE
    PS> ./Update-AppxManifest.ps1 -Identity Sideload

    Updates the manifest using the developer sideload identity and display name.

.EXAMPLE
    PS> ./Update-AppxManifest.ps1 -Identity SideloadUnsigned

    Updates the manifest using the developer identity, display name, and an unsigned
    publisher value for frictionless sideloaded deployments.

.NOTES
    The script is intended for UWP projects.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [Alias('id')]
    [ValidateSet('Store', 'Sideload', 'SideloadUnsigned')]
    [string]$Identity = 'Store'
)

New-Variable -Name StoreIdentityName -Value "18496Starpine.Screenbox" -Option Constant
New-Variable -Name StoreIdentityPublisher -Value "CN=ABCDF790-DBE4-48F7-8204-32FCB69ADF9C" -Option Constant
New-Variable -Name StoreDisplayName -Value "Screenbox" -Option Constant

New-Variable -Name UnsignedOid -Value "2.25.311729368913984317654407730594956997722=1" -Option Constant

$repositoryPath = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path -Path $repositoryPath -ChildPath "Screenbox/Package.appxmanifest"

switch ($Identity) {
    'Store' {
        $identityName = $StoreIdentityName
        $displayName = $StoreDisplayName
        $visualDisplayName = "ms-resource:ManifestResources/AppDisplayName"
        break
    }
    'Sideload' {
        $identityName = "$StoreIdentityName.Dev"
        $displayName = "$StoreDisplayName Dev"
        $visualDisplayName = "$StoreDisplayName Dev"
        break
    }
    'SideloadUnsigned' {
        $identityName = "$StoreIdentityName.Dev"
        $displayName = "$StoreDisplayName Dev"
        $visualDisplayName = "$StoreDisplayName Dev"
        break
    }
}

[xml]$xmlDoc = Get-Content -Path $manifestPath -Raw

$xmlDoc.Package.Identity.Name = $identityName

if ($Identity -eq 'SideloadUnsigned') {
    $xmlDoc.Package.Identity.Publisher = "$StoreIdentityPublisher, OID.$UnsignedOid"
}

$xmlDoc.Package.Properties.DisplayName = $displayName
$xmlDoc.Package.Applications.Application.VisualElements.DisplayName = $visualDisplayName

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
