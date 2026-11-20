<#
.SYNOPSIS
    Formats or validates the XAML files according to the project formatting style.

.DESCRIPTION
    The Format-Xaml.ps1 script processes all *.xaml files in the repository,
    excluding the obj and bin folders.

    By default, the script formats files in place. When Validate is specified,
    the script checks whether files conform to the project formatting style
    without modifying them and returns a non-zero exit code if any files require
    formatting.

.PARAMETER Validate
    Validates that XAML files conform to the project formatting style. No files
    are modified. The script returns a non-zero exit code if formatting changes
    would be required.

.EXAMPLE
    PS> ./scripts/Format-Xaml.ps1

    Formats all XAML files in place.

.EXAMPLE
    PS> ./scripts/Format-Xaml.ps1 -Validate

    Reports each file as PASS or FAIL and returns a non-zero exit
    code if any files require formatting.

.NOTES
    The script requires the XamlStyler.Console .NET tool.
#>

[CmdletBinding()]
param(
    [switch]$Validate
)

$repositoryPath = Split-Path -Parent $PSScriptRoot
$xamlFiles = Get-ChildItem -Path $repositoryPath -Filter *.xaml -Recurse -File |
    Where-Object { $_.DirectoryName -notmatch '([\\/](obj|bin)([\\/]|$))' } |
    Select-Object -ExpandProperty FullName

if ($xamlFiles.count -eq 0) {
    exit 0
}

if ($Validate) {
    dotnet tool run xstyler --passive --file ($xamlFiles -join ',')
} else {
    dotnet tool run xstyler --file ($xamlFiles -join ',')
}

exit $LASTEXITCODE
