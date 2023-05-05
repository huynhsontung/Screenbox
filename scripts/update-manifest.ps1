[CmdletBinding()]
param (
  [Parameter(Mandatory=$false)]
  [string]
  $Version
)

# Update package manifest for store upload
$ProjectPath = "$PSScriptRoot\..\Screenbox"
$ManifestPath = "$ProjectPath\Package.appxmanifest"
[xml]$xmlDoc = Get-Content $ManifestPath
$xmlDoc.Package.Identity.Name="18496Starpine.Screenbox"
$xmlDoc.Package.Identity.Publisher="CN=ABCDF790-DBE4-48F7-8204-32FCB69ADF9C"
$xmlDoc.Package.Properties.DisplayName="Screenbox"
$xmlDoc.Package.Applications.Application.VisualElements.DisplayName="ms-resource:ManifestResources/AppDisplayName"
if ($Version -match '^(\d+.\d+.\d+)$') {
  $Version = $Version + ".0"
  $xmlDoc.Package.Identity.Version = $Version
}

$xmlDoc.Save($ManifestPath)
