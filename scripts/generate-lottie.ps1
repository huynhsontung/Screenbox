$RepositoryRoot = Split-Path -Parent $PSScriptRoot
$InputFolder = Join-Path $RepositoryRoot 'assets\animations'
$OutputFolder = Join-Path $RepositoryRoot 'Screenbox\Assets\Animations'
$Namespace = 'Screenbox.Animations'

$files = Get-ChildItem -Path $InputFolder -Filter '*.json' -File -Recurse -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName

if ($files.count -gt 0)
{
  foreach ($file in $files)
  {
    dotnet tool run LottieGen `
      -GenerateColorBindings `
      -GenerateDependencyObject `
      -Language CSharp `
      -MinimumUapVersion 8 `
      -Namespace $Namespace `
      -Public `
      -WinUIVersion 2.8 `
      -InputFile $file `
      -OutputFolder $OutputFolder
  }
}
else
{
  Write-Host "No JSON files were found in '$InputFolder'"
  exit 0
}
