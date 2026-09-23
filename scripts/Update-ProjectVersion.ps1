<#
.SYNOPSIS
    Updates project version metadata and keeps the package manifest in sync.

.DESCRIPTION
    The Update-ProjectVersion.ps1 script updates or creates the Version and Copyright
    elements in the first appropriate <PropertyGroup> of the Screenbox.csproj file.
    It also updates the Identity version attribute in the Package.appxmanifest file
    to ensure both files remain aligned.

.PARAMETER Version
    Specifies the version to apply to the project and manifest. The value must follow
    the Major.Minor.Build.Revision format (for example, 1.2.3.4), with each segment
    represented as an integer without leading zeros.

    If the parameter is not specified, the script generates a version based on the
    current date:

    Major    = 0
    Minor    = YYMM (last two digits of the year and month)
    Build    = DD (day)
    Revision = 0

    The value is normalized to the .NET versioning format required by the project.

.EXAMPLE
    PS> ./Update-ProjectVersion.ps1 -Version 1.2.3.4

.EXAMPLE
    PS> ./Update-ProjectVersion.ps1

.NOTES
    The script is intended for SDK-style UWP .NET projects.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [Alias("V")]
    [ValidatePattern('^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$')]
    [string]$Version
)

function Get-GeneratedVersion {
    $currentDate = Get-Date
    #$major = [int]$currentDate.ToString("yy")
    $minor = [int]$currentDate.ToString("yyMM")
    $build = [int]$currentDate.ToString("dd")
    #$revision = [int]$currentDate.ToString("HHmm")
    return "0.$minor.$build.0"
}

$repositoryPath = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryPath -ChildPath "Screenbox/Screenbox.csproj"
$manifestPath = Join-Path $repositoryPath -ChildPath "Screenbox/Package.appxmanifest"

$appVersion = if ($Version) { $Version } else { Get-GeneratedVersion }

$currentYear = (Get-Date).Year
$copyrightText = "Copyright 2022-$currentYear Tung Huynh"

[xml]$projectXml = Get-Content -Path $projectPath -Raw

# Select the first PropertyGroup that contains Version or Copyright
$propertyGroup = $projectXml.Project.PropertyGroup |
    Where-Object { $_.Version -or $_.Copyright } |
    Select-Object -First 1

# If none found, select the first PropertyGroup
if (-not $propertyGroup) {
    $propertyGroup = $projectXml.Project.PropertyGroup |
        Select-Object -First 1
}

# If still none found, create a new PropertyGroup
if (-not $propertyGroup) {
    $propertyGroup = $projectXml.CreateElement("PropertyGroup")
    $projectXml.Project.AppendChild($propertyGroup) | Out-Null
}

# Ensure Version element exists and update its value
if (-not $propertyGroup.Version) {
    $versionElement = $projectXml.CreateElement("Version")
    $propertyGroup.AppendChild($versionElement) | Out-Null
}

$propertyGroup.Version = $appVersion

# Ensure Copyright element exists and update its value
if (-not $propertyGroup.Copyright) {
    $copyrightElement = $projectXml.CreateElement("Copyright")
    $propertyGroup.AppendChild($copyrightElement) | Out-Null
}

$propertyGroup.Copyright = $copyrightText

# Update the Package.appxmanifest version to match the project version
[xml]$manifestXml = Get-Content -Path $manifestPath -Raw

$manifestXml.Package.Identity.Version = $appVersion

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$settings.Indent = $true
#$settings.NewLineChars = "`r`n"

$projectWriter = [System.Xml.XmlWriter]::Create($projectPath, $settings)
$manifestWriter = [System.Xml.XmlWriter]::Create($manifestPath, $settings)

try {
    $projectXml.Save($projectWriter)
    $manifestXml.Save($manifestWriter)
}
finally {
    $projectWriter.Dispose()
    $manifestWriter.Dispose()
}
