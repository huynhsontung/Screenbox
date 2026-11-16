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
$files = Get-ChildItem -Path $repositoryPath -Filter *.xaml -Recurse | Select-Object -ExpandProperty FullName | Where-Object { $_ -notmatch "(\\obj\\)|(\\bin\\)" }

if ($files.count -eq 0) {
    exit 0
}

dotnet tool run xstyler --file ($files -join ',')
exit $LASTEXITCODE
