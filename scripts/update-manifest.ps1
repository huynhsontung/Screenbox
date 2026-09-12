[CmdletBinding()]
param (
    [Parameter(Mandatory=$false)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

New-Variable -Name IdentityName -Value "18496Starpine.Screenbox" -Option Constant
New-Variable -Name IdentityPublisher -Value 'CN=ABCDF790-DBE4-48F7-8204-32FCB69ADF9C' -Option Constant
New-Variable -Name IdentityPublisherUnsigned -Value "$IdentityPublisher, OID.2.25.311729368913984317654407730594956997722=1" -Option Constant

New-Variable -Name DisplayName -Value "Screenbox" -Option Constant
New-Variable -Name VisualDisplayName -Value "ms-resource:ManifestResources/AppDisplayName" -Option Constant

$repoPath = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path -Path $repoPath -ChildPath "Screenbox/Package.appxmanifest"

[xml]$xmlDoc = Get-Content -Path $manifestPath
$xmlDoc.Package.Identity.Name = $IdentityName

if ($PSBoundParameters.ContainsKey('Version')) {
    $xmlDoc.Package.Identity.Publisher = $IdentityPublisher
    $xmlDoc.Package.Identity.Version = "$Version.0"
}
else {
    $xmlDoc.Package.Identity.Publisher = $IdentityPublisherUnsigned
}

$xmlDoc.Package.Properties.DisplayName = $DisplayName
$xmlDoc.Package.Applications.Application.VisualElements.DisplayName = $VisualDisplayName

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$settings.Indent = $true

$writer = [System.Xml.XmlWriter]::Create($manifestPath, $settings)

try {
    $xmlDoc.Save($writer)
}
finally {
    $writer.Dispose()
}
