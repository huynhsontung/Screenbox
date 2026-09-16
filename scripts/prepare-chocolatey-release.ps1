[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version,

    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string] $AppVersion,

    [Parameter(Mandatory)]
    [ValidatePattern('^v\d+\.\d+\.\d+$')]
    [string] $ReleaseTag,

    [Parameter(Mandatory)]
    [ValidatePattern('^https://github\.com/huynhsontung/Screenbox/releases/tag/v\d+\.\d+\.\d+$')]
    [string] $ReleaseUrl,

    [Parameter(Mandatory)]
    [ValidatePattern('^https://github\.com/huynhsontung/Screenbox/releases/download/v\d+\.\d+\.\d+/.+\.msixbundle$')]
    [string] $BundleUrl,

    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9A-Fa-f]{64}$')]
    [string] $BundleChecksum,

    [Parameter(Mandatory)]
    [string] $SourceDirectory,

    [Parameter(Mandatory)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

$expectedAppVersion = "$Version.0"
if ($AppVersion -ne $expectedAppVersion) {
    throw "AppVersion '$AppVersion' does not match Chocolatey version '$Version'. Expected '$expectedAppVersion'."
}

$expectedTag = "v$Version"
if ($ReleaseTag -ne $expectedTag) {
    throw "ReleaseTag '$ReleaseTag' does not match version '$Version'. Expected '$expectedTag'."
}

$expectedReleaseUrl = "https://github.com/huynhsontung/Screenbox/releases/tag/$expectedTag"
if ($ReleaseUrl -ne $expectedReleaseUrl) {
    throw "ReleaseUrl '$ReleaseUrl' does not match version '$Version'. Expected '$expectedReleaseUrl'."
}

$expectedBundlePrefix = "https://github.com/huynhsontung/Screenbox/releases/download/$expectedTag/"
if (-not $BundleUrl.StartsWith($expectedBundlePrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "BundleUrl '$BundleUrl' is not an asset of release '$expectedTag'."
}

if (-not $BundleUrl.EndsWith('.msixbundle', [StringComparison]::OrdinalIgnoreCase)) {
    throw "BundleUrl '$BundleUrl' is not an .msixbundle asset."
}

$source = (Resolve-Path -LiteralPath $SourceDirectory).Path
$nuspecSource = Join-Path $source 'screenbox.nuspec'
$installSource = Join-Path $source 'tools/chocolateyInstall.ps1'
$uninstallSource = Join-Path $source 'tools/chocolateyUninstall.ps1'
foreach ($requiredFile in @($nuspecSource, $installSource, $uninstallSource)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required Chocolatey package file not found: $requiredFile"
    }
}

if (Test-Path -LiteralPath $OutputDirectory) {
    Remove-Item -LiteralPath $OutputDirectory -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $OutputDirectory -Recurse -Force

$nuspecPath = Join-Path $OutputDirectory 'screenbox.nuspec'
$installPath = Join-Path $OutputDirectory 'tools/chocolateyInstall.ps1'

# Update the copied nuspec as XML so metadata changes are not dependent on line
# formatting. choco pack also receives --version in CI as a second safeguard.
[xml]$nuspec = Get-Content -LiteralPath $nuspecPath -Raw
$namespace = [Xml.XmlNamespaceManager]::new($nuspec.NameTable)
$namespace.AddNamespace('n', 'http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd')
$metadata = $nuspec.SelectSingleNode('/n:package/n:metadata', $namespace)
if (-not $metadata) {
    throw 'screenbox.nuspec is missing its metadata element.'
}

function Set-NuspecValue {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Value
    )

    $node = $metadata.SelectSingleNode("n:$Name", $namespace)
    if (-not $node) {
        throw "screenbox.nuspec is missing required metadata element '$Name'."
    }
    $node.InnerText = $Value
}

Set-NuspecValue -Name 'version' -Value $Version
Set-NuspecValue -Name 'releaseNotes' -Value $ReleaseUrl
Set-NuspecValue -Name 'packageSourceUrl' -Value 'https://github.com/huynhsontung/Screenbox/tree/main/packaging/chocolatey'

$xmlSettings = [Xml.XmlWriterSettings]::new()
$xmlSettings.Encoding = [Text.UTF8Encoding]::new($false)
$xmlSettings.Indent = $true
$xmlSettings.NewLineChars = "`n"
$xmlSettings.NewLineHandling = [Xml.NewLineHandling]::Replace
$writer = [Xml.XmlWriter]::Create($nuspecPath, $xmlSettings)
try {
    $nuspec.Save($writer)
}
finally {
    $writer.Dispose()
}

$installText = [IO.File]::ReadAllText($installPath)

function Replace-SingleMatch {
    param(
        [Parameter(Mandatory)] [string] $Text,
        [Parameter(Mandatory)] [string] $Pattern,
        [Parameter(Mandatory)] [string] $Replacement,
        [Parameter(Mandatory)] [string] $Description
    )

    $regex = [Text.RegularExpressions.Regex]::new($Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
    $matches = $regex.Matches($Text)
    if ($matches.Count -ne 1) {
        throw "Expected exactly one $Description assignment in chocolateyInstall.ps1; found $($matches.Count)."
    }

    return $regex.Replace($Text, $Replacement, 1)
}

$installText = Replace-SingleMatch `
    -Text $installText `
    -Pattern '^\$appVersion\s*=\s*\[version\]"[^"]+"\s*$' `
    -Replacement ('$appVersion = [version]"{0}"' -f $AppVersion) `
    -Description 'appVersion'

$installText = Replace-SingleMatch `
    -Text $installText `
    -Pattern '^\$bundleUrl\s*=\s*"[^"]+"\s*$' `
    -Replacement ('$bundleUrl = "{0}"' -f $BundleUrl) `
    -Description 'bundleUrl'

$installText = Replace-SingleMatch `
    -Text $installText `
    -Pattern '^\$bundleChecksum\s*=\s*"[0-9A-Fa-f]+"\s*$' `
    -Replacement ('$bundleChecksum = "{0}"' -f $BundleChecksum.ToLowerInvariant()) `
    -Description 'bundleChecksum'

# The local temporary filename is deliberately release-independent. This avoids
# another value that would otherwise have to change for every Screenbox version.
$installText = Replace-SingleMatch `
    -Text $installText `
    -Pattern '^\$bundlePath\s*=\s*Join-Path\s+\$workDir\s+"[^"]+\.msixbundle"\s*$' `
    -Replacement '$bundlePath = Join-Path $workDir "Screenbox.msixbundle"' `
    -Description 'bundlePath'

[IO.File]::WriteAllText($installPath, $installText, [Text.UTF8Encoding]::new($false))

# Validate the generated source before allowing choco pack to run.
[xml]$generatedNuspec = Get-Content -LiteralPath $nuspecPath -Raw
$generatedNamespace = [Xml.XmlNamespaceManager]::new($generatedNuspec.NameTable)
$generatedNamespace.AddNamespace('n', 'http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd')

$checks = @{
    version = $Version
    releaseNotes = $ReleaseUrl
    packageSourceUrl = 'https://github.com/huynhsontung/Screenbox/tree/main/packaging/chocolatey'
}
foreach ($entry in $checks.GetEnumerator()) {
    $actual = $generatedNuspec.SelectSingleNode("/n:package/n:metadata/n:$($entry.Key)", $generatedNamespace).InnerText
    if ($actual -ne $entry.Value) {
        throw "Generated nuspec '$($entry.Key)' value '$actual' does not match expected '$($entry.Value)'."
    }
}

$generatedInstall = [IO.File]::ReadAllText($installPath)
$requiredValues = @(
    ('$appVersion = [version]"{0}"' -f $AppVersion),
    ('$bundleUrl = "{0}"' -f $BundleUrl),
    ('$bundleChecksum = "{0}"' -f $BundleChecksum.ToLowerInvariant()),
    '$bundlePath = Join-Path $workDir "Screenbox.msixbundle"'
)
foreach ($requiredValue in $requiredValues) {
    if (-not $generatedInstall.Contains($requiredValue, [StringComparison]::Ordinal)) {
        throw "Generated chocolateyInstall.ps1 is missing expected value: $requiredValue"
    }
}

Write-Host "Prepared Chocolatey package source for Screenbox $Version at '$OutputDirectory'."
