$repositoryRoot = Split-Path -Parent $PSScriptRoot
$inputPath = Join-Path $RepositoryRoot 'assets\animations'
$outputPath = Join-Path $RepositoryRoot 'Screenbox\Assets\Animations'
$namespace = 'Screenbox.Animations'

$files = Get-ChildItem -Path $inputPath -Filter '*.json' -File -Recurse -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName

if ($files.count -gt 0) {
  foreach ($file in $files) {
    dotnet tool run LottieGen `
      -GenerateColorBindings `
      -GenerateDependencyObject `
      -Language CSharp `
      -MinimumUapVersion 8 `
      -Namespace $namespace `
      -Public `
      -WinUIVersion 2.8 `
      -InputFile $file `
      -OutputFolder $outputFolder
  }
}
else {
  Write-Host "No .json files found in the folder '$inputPath'."
  exit 0
}
