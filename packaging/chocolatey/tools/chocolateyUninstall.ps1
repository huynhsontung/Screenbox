$ErrorActionPreference = "Stop"

$packageName = if ($env:ChocolateyPackageName) { $env:ChocolateyPackageName } else { "screenbox" }
$appPackageName = "18496Starpine.Screenbox"

$installedPackages = @(Get-AppxPackage -Name $appPackageName -ErrorAction SilentlyContinue)
if ($installedPackages.Count -eq 0) {
    Write-Warning "Screenbox is not installed for the current user."
    return
}

foreach ($installed in $installedPackages) {
    Write-Host "Removing Screenbox $($installed.Version)..."
    Remove-AppxPackage -Package $installed.PackageFullName -ErrorAction Stop
}

$remaining = @(Get-AppxPackage -Name $appPackageName -ErrorAction SilentlyContinue)
if ($remaining.Count -gt 0) {
    throw "Screenbox uninstall verification failed."
}

Write-Host "Screenbox uninstalled successfully."
