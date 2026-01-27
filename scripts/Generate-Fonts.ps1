$repositoryPath = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path -Path $repositoryPath -ChildPath "assets\fonts\fontforge-ufo-to-ttf.pe"
$mdl2PackagePath = Join-Path -Path $repositoryPath -ChildPath "assets\fonts\ScreenboxMDL2Assets.ufo"
$fluentPackagePath = Join-Path -Path $repositoryPath -ChildPath "assets\fonts\ScreenboxFluentIcons.ufo"

& "fontforge" -script $scriptPath $mdl2PackagePath $fluentPackagePath
