[CmdletBinding()]
param (
  [Parameter(Mandatory=$false)]
  [ValidatePattern('^\d+\.\d+\.\d+$')]
  [string]
  $Version
)

$repoPath = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path -Path $repoPath -ChildPath "Screenbox/Package.appxmanifest"

[xml]$xmlDoc = Get-Content -Path $manifestPath
$xmlDoc.Package.Identity.Name="18496Starpine.Screenbox"
$xmlDoc.Package.Properties.DisplayName="Screenbox"
$xmlDoc.Package.Applications.Application.VisualElements.DisplayName="ms-resource:ManifestResources/AppDisplayName"

$publisher = 'CN=ABCDF790-DBE4-48F7-8204-32FCB69ADF9C'
$publisherUnsigned = "$publisher, OID.2.25.311729368913984317654407730594956997722=1"

if ($PSBoundParameters.ContainsKey('Version')) {
  $xmlDoc.Package.Identity.Publisher = $publisher
  $xmlDoc.Package.Identity.Version = $Version + ".0"
}
else {
  $xmlDoc.Package.Identity.Publisher = $publisherUnsigned
}

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$settings.Indent = $true

$writer = [System.Xml.XmlWriter]::Create($manifestPath, $settings)
$xmlDoc.Save($writer)
$writer.Close()
