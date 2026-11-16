<#
.SYNOPSIS
    Formats the XAML files to conform to the project formatting style.

.DESCRIPTION
    The Format-Xaml.ps1 script formats all *.xaml files in the repository,
    excluding the obj and bin folders. Files are modified in place.

.EXAMPLE
    PS> ./scripts/Format-Xaml.ps1

.NOTES
    The script requires the XamlStyler.Console .NET tool.
#>

$repositoryPath = Split-Path -Parent $PSScriptRoot
$xamlFiles = Get-ChildItem -Path $repositoryPath -Filter *.xaml -Recurse -File |
    Where-Object { $_.DirectoryName -notmatch '([\\/](obj|bin)([\\/]|$))' }
    Select-Object -ExpandProperty FullName

if ($xamlFiles.count -eq 0) {
    exit 0
}

dotnet tool run xstyler --file ($xamlFiles -join ',')
exit $LASTEXITCODE
