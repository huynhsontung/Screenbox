$ErrorActionPreference = "Stop"

$packageName = if ($env:ChocolateyPackageName) { $env:ChocolateyPackageName } else { "screenbox" }
$appPackageName = "18496Starpine.Screenbox"
$appVersion = [version]"0.21.0.0"

$bundleUrl = "https://github.com/huynhsontung/Screenbox/releases/download/v0.21.0/Screenbox_0.21.0.0_neutral.msixbundle"
$bundleChecksum = "6cdcd7672d26825bd6e24f5aefcec6fe140ce27742a49f02fc2f67d702d7c13f"

$xamlUrl = "https://www.nuget.org/api/v2/package/Microsoft.UI.Xaml/2.8.7"
$xamlChecksum = "79207b10fe243eb1a8dcdc29beceba2f472f145ed31f147f7ae3f43b0659c9f7"

$vclibsUrl = "https://github.com/microsoft/winget-cli/releases/download/v1.12.350/DesktopAppInstaller_Dependencies.zip"
$vclibsChecksum = "906cad3b2be067d816b20ea4eb1df541f8a23ac4a4aa9fed70ce675cd918e6a6"

if ([Environment]::OSVersion.Version.Build -lt 19041) {
    throw "Screenbox requires Windows 10 version 2004 / build 19041 or later."
}

$nativeArch = if ($env:PROCESSOR_ARCHITEW6432) { $env:PROCESSOR_ARCHITEW6432 } else { $env:PROCESSOR_ARCHITECTURE }
switch ($nativeArch.ToUpperInvariant()) {
    "AMD64" { $arch = "x64" }
    "ARM64" { $arch = "arm64" }
    "X86"   { $arch = "x86" }
    default { throw "Unsupported Windows architecture: $nativeArch" }
}

$installed = Get-AppxPackage -Name $appPackageName -ErrorAction SilentlyContinue |
    Sort-Object Version -Descending |
    Select-Object -First 1

if ($installed) {
    $installedVersion = [version]$installed.Version
    if ($installedVersion -ge $appVersion) {
        Write-Warning "Screenbox $installedVersion is already installed. No application files need to be changed."
        return
    }
}

$workDir = Join-Path ([IO.Path]::GetTempPath()) ("chocolatey-screenbox-" + [guid]::NewGuid().ToString("N"))
$bundlePath = Join-Path $workDir "Screenbox.msixbundle"
$xamlPackagePath = Join-Path $workDir "Microsoft.UI.Xaml.2.8.7.nupkg"
$vclibsPackagePath = Join-Path $workDir "DesktopAppInstaller_Dependencies.zip"
$xamlAppxPath = Join-Path $workDir "Microsoft.UI.Xaml.2.8.$arch.appx"
$vclibsAppxPath = Join-Path $workDir "Microsoft.VCLibs.140.00.$arch.appx"

New-Item -ItemType Directory -Force -Path $workDir | Out-Null

try {
    Get-ChocolateyWebFile -PackageName $packageName -Url $bundleUrl -FileFullPath $bundlePath -Checksum $bundleChecksum -ChecksumType "sha256"
    Get-ChocolateyWebFile -PackageName $packageName -Url $xamlUrl -FileFullPath $xamlPackagePath -Checksum $xamlChecksum -ChecksumType "sha256"
    Get-ChocolateyWebFile -PackageName $packageName -Url $vclibsUrl -FileFullPath $vclibsPackagePath -Checksum $vclibsChecksum -ChecksumType "sha256"

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $xamlZip = [IO.Compression.ZipFile]::OpenRead($xamlPackagePath)
    try {
        $xamlEntry = $xamlZip.Entries | Where-Object {
            $_.FullName -match ("^tools/AppX/{0}/Release/Microsoft\.UI\.Xaml\.2\.8\.appx$" -f [regex]::Escape($arch))
        } | Select-Object -First 1

        if (-not $xamlEntry) {
            throw "Microsoft.UI.Xaml 2.8.7 dependency for $arch was not found."
        }

        [IO.Compression.ZipFileExtensions]::ExtractToFile($xamlEntry, $xamlAppxPath, $true)
    }
    finally {
        $xamlZip.Dispose()
    }

    $vclibsZip = [IO.Compression.ZipFile]::OpenRead($vclibsPackagePath)
    try {
        $vclibsEntry = $vclibsZip.Entries | Where-Object {
            $_.FullName -match ("^{0}/Microsoft\.VCLibs\.140\.00_14\.0\.33519\.0_{0}\.appx$" -f [regex]::Escape($arch))
        } | Select-Object -First 1

        if (-not $vclibsEntry) {
            throw "Microsoft.VCLibs 14.0.33519.0 dependency for $arch was not found."
        }

        [IO.Compression.ZipFileExtensions]::ExtractToFile($vclibsEntry, $vclibsAppxPath, $true)
    }
    finally {
        $vclibsZip.Dispose()
    }

    $dependencyPaths = @($xamlAppxPath, $vclibsAppxPath)

    Write-Host "Installing Screenbox $appVersion for $nativeArch using architecture-matched Microsoft framework dependencies."
    Add-AppxPackage -Path $bundlePath -DependencyPath $dependencyPaths -ForceApplicationShutdown -ErrorAction Stop

    $installed = Get-AppxPackage -Name $appPackageName -ErrorAction SilentlyContinue |
        Sort-Object Version -Descending |
        Select-Object -First 1

    if (-not $installed -or [version]$installed.Version -lt $appVersion) {
        throw "Screenbox installation verification failed."
    }

    Write-Host "Screenbox $($installed.Version) installed successfully."
}
finally {
    Remove-Item -Path $workDir -Recurse -Force -ErrorAction SilentlyContinue
}
