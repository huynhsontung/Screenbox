<#
.SYNOPSIS
    Converts Lottie .json animation files to C# classes using LottieGen.

.DESCRIPTION
    The ConvertFrom-Lottie.ps1 script convert a Lottie (JSON) animation file to a C# class.

.PARAMETER Path
    Specifies the path to the Lottie animation files that you want to convert to C# code.

    This parameter accepts wildcard characters. Wildcard characters allow you to add all
    files in a directory to your input.

    If the path is not specified, all *.json files under <repo>\Screenbox\assets\animations
    are processed.

.PARAMETER DestinationPath
    Specifies the path to the generated C# output files.

    If the path is not specified, the default destination is <repo>\Screenbox\Assets\Animations.

.PARAMETER Namespace
    Specifies the C# namespace used for the generated C# classes.

    If the namespace is not specified, the default namespace is Screenbox.Animations.

.EXAMPLE
    PS> ./scripts/ConvertFrom-Lottie.ps1

.EXAMPLE
    PS> ./scripts/ConvertFrom-Lottie.ps1 -Path "D:/Screenbox/assets/animations/*" -DestinationPath "D:/Screenbox/Screenbox/Assets/Animations"

.NOTES
    The script requires the LottieGen .NET tool to be installed.
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [SupportsWildcards()]
    [string[]]$Path,

    [Parameter(Mandatory = $false)]
    [string]$DestinationPath,

    [Parameter(Mandatory = $false)]
    [string]$Namespace = 'Screenbox.Animations'
)

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $Path) {
    $Path = Join-Path $repositoryRoot 'assets/animations/*.json'
}

if (-not $DestinationPath) {
    $DestinationPath = Join-Path $repositoryRoot 'Screenbox/Assets/Animations'
}

New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null

$lottieFiles = Get-ChildItem -Path $Path -File -Recurse -ErrorAction Stop |
    Where-Object Extension -eq '.json' |
    Select-Object -ExpandProperty FullName -Unique

if (-not $lottieFiles) {
    Write-Verbose 'No .json files were found.'
    return
}

foreach ($file in $lottieFiles) {
    dotnet tool run LottieGen `
        -GenerateColorBindings `
        -GenerateDependencyObject `
        -Language CSharp `
        -MinimumUapVersion 8 `
        -Namespace $Namespace `
        -Public `
        -WinUIVersion 2.8 `
        -InputFile $file `
        -OutputFolder $DestinationPath

    if ($LASTEXITCODE -ne 0) {
        throw "LottieGen failed for '$file'."
    }
}
