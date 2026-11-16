<#
.SYNOPSIS
    Tests whether the XAML files are valid and conform to the project formatting style

.DESCRIPTION
    The Test-XamlFormat.ps1 script validates all *.xaml files in the repository,
    excluding the obj and bin folders. It runs xstyler in passive mode, and reports
    each file as PASS or FAIL.

    The script exits with 0 if all files pass; otherwise, it exits with a non-zero code.

    Run Format-Xaml.ps1 to fix failures.

.EXAMPLE
    PS> ./scripts/Test-XamlFormat.ps1

.NOTES
    The script requires the XamlStyler.Console .NET tool.
#>

$repositoryPath = Split-Path -Parent $PSScriptRoot
$xamlFiles = Get-ChildItem -Path $repositoryPath -Filter *.xaml -Recurse -File |
    Where-Object { $_.DirectoryName -notmatch '([\\/](obj|bin)[\\/])' } |
    Select-Object -ExpandProperty FullName

if ($xamlFiles.count -eq 0) {
    exit 0
}

dotnet tool run xstyler --passive --file ($xamlFiles -join ',')
exit $LASTEXITCODE
