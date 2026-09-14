<#
.SYNOPSIS
    Updates the Version in the first PropertyGroup of Directory.Build.props.

.DESCRIPTION
    The Update-ProjectVersion.ps1 script updates or creates the Version element
    in the first <PropertyGroup> of the Directory.Build.props file.

.PARAMETER Version
    Specifies the version to assign to the Version element. The value must be in the
    format Major.Minor.Build.Revision (e.g., 1.2.3.4), where each segment is a integer
    without leading zeros.

    If the parameter is not specified, the script generates a version based on the
    current date and time using the following values:

    Major    = 0
    Minor    = YY (last two digits of the year)
    Build    = MMDD (month and day)
    Revision = HHmm (hour and minute in 24-hour format)

    All generated segments are normalized to integers to ensure compatibility with
    .NET versioning rules.

.EXAMPLE
    PS> ./Update-ProjectVersion.ps1 -Version 1.2.3.4

.EXAMPLE
    PS> ./Update-ProjectVersion.ps1

.NOTES
    The script is intended for SDK-style .NET projects where Directory.Build.props
    controls shared versioning.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory=$false)]
    [ValidatePattern('^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$')]
    [string]$Version
)

$repositoryPath = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $repositoryPath "Directory.Build.props"

if ($Version) {
    $projectVersion = $Version
} else {
    $currentDate = Get-Date
    #$major = [int]$currentDate.ToString("yy")
    $minor = [int]$currentDate.ToString("yy")
    $build = [int]$currentDate.ToString("MMdd")
    $revision = [int]$currentDate.ToString("HHmm")
    $projectVersion = "0.$minor.$build.$revision"
}

$currentYear = (Get-Date).Year
$copyrightText = "Copyright 2022-$currentYear Tung Huynh"

[xml]$propsXml = Get-Content -Path $propsPath -Raw

$propertyGroup = $propsXml.Project.PropertyGroup |
    Where-Object { $_.Version } |
    Select-Object -First 1

if (-not $propertyGroup) {
    $propertyGroup = $propsXml.Project.PropertyGroup |
        Select-Object -First 1
}

if (-not $propertyGroup) {
    $propertyGroup = $propsXml.CreateElement("PropertyGroup")
    $propsXml.Project.AppendChild($propertyGroup) | Out-Null
}

if (-not $propertyGroup.Version) {
    $versionElement = $propsXml.CreateElement("Version")
    $propertyGroup.AppendChild($versionElement) | Out-Null
}

$propertyGroup.Version = $projectVersion

if (-not $propertyGroup.Copyright) {
    $copyrightElement = $propsXml.CreateElement("Copyright")
    $propertyGroup.AppendChild($copyrightElement) | Out-Null
}

$propertyGroup.Copyright = $copyrightText

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$settings.Indent = $true
#$settings.NewLineChars = "`r`n"

$writer = [System.Xml.XmlWriter]::Create($propsPath, $settings)

try {
    $propsXml.Save($writer)
}
finally {
    $writer.Dispose()
}
