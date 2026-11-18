<#
.SYNOPSIS
    Tests or formats the XAML files according to the project formatting style.

.DESCRIPTION
    The Test-XamlFormat.ps1 script processes all *.xaml files in the repository,
    excluding the obj and bin folders. By default, it runs xstyler in passive mode,
    reports each file as PASS or FAIL, and exits non-zero if any file fails.

    With the Format switch, files are modified in place instead.

.PARAMETER Format
    Formats the XAML files in place instead of running in passive validation mode.

.EXAMPLE
    PS> ./scripts/Test-XamlFormat.ps1

.EXAMPLE
    PS> ./scripts/Test-XamlFormat.ps1 -Format

.NOTES
    The script requires the XamlStyler.Console .NET tool.
#>

[CmdletBinding()]
param(
    [switch]$Format
)

$repositoryPath = Split-Path -Parent $PSScriptRoot
$xamlFiles = Get-ChildItem -Path $repositoryPath -Filter *.xaml -Recurse -File |
    Where-Object { $_.DirectoryName -notmatch '([\\/](obj|bin)([\\/]|$))' } |
    Select-Object -ExpandProperty FullName

if ($xamlFiles.count -eq 0) {
    exit 0
}

if ($Format) {
    dotnet tool run xstyler --file ($xamlFiles -join ',')
} else {
    dotnet tool run xstyler --passive --file ($xamlFiles -join ',')
}

exit $LASTEXITCODE
