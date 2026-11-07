<#
.SYNOPSIS
  Builds the local Automatica demo image (automatica-demo:local) from this source tree.

.DESCRIPTION
  Everything is built on the host and copied into a slim aspnet image (Dockerfile.local):
    1. packs the Automatica.Core.* libraries the plugins reference (version $CoreVersion, local feed)
    2. publishes Automatica.Core for linux-x64 with the same version
    3. publishes the demo drivers and logics into the plugin layout the core loads from
       (<plugins>/Drivers/<ComponentName>, <plugins>/Rules/<ComponentName>)
    4. builds the Angular frontend (Automatica.WebNew) into wwwroot
    5. docker build -f Dockerfile.local

  All build output stays in docker/demo/.build and in the projects' bin/obj folders of this tree.
  The frontend needs node_modules and src/devextreme-license.ts in Automatica.WebNew (not part of the repository).

.EXAMPLE
  ./docker/demo/build-local.ps1
  ./docker/demo/build-local.ps1 -SkipFrontend -SkipCore
#>
[CmdletBinding()]
param(
    [string]$CoreVersion = "10.0.0.9100",
    [string]$Tag = "automatica-demo:local",
    [string]$Configuration = "Release",
    [switch]$SkipCore,
    [switch]$SkipPlugins,
    [switch]$SkipFrontend,
    [switch]$SkipImage
)

$ErrorActionPreference = "Stop"

$demoDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $demoDir "..\..")).Path
$coreSrc = Join-Path $repoRoot "src\automatica.core"
$buildDir = Join-Path $demoDir ".build"
$feedDir = Join-Path $buildDir "feed"
$appDir = Join-Path $buildDir "app"
$pluginDir = Join-Path $buildDir "plugins"
$pluginTargets = Join-Path $demoDir "build\demo-plugin.targets"

# drivers that run without hardware (relative to src/automatica.drivers)
$drivers = @(
    "automatica.driver.constants\P3.Driver.Constants\P3.Driver.Constants.csproj",
    "automatica.driver.times\P3.Driver.Times.DriverFactory\P3.Driver.Times.DriverFactory.csproj",
    "automatica.driver.machineflags\P3.Driver.MachineFlags\P3.Driver.MachineFlags.csproj",
    "automatica.driver.blockchain.ticker\P3.Driver.Blockchain.Ticker.Driver\P3.Driver.Blockchain.Ticker.Driver.csproj",
    "automatica.driver.huebridgesimulator\HueBridgeSimulator\P3.Driver.HueBridgeSimulatorDriverFactory\P3.Driver.HueBridgeSimulator.DriverFactory.csproj",
    "automatica.driver.homekit\P3.Driver.HomeKitFactory\P3.Driver.HomeKitFactory.csproj",
    "automatica.driver.matter\P3.Driver.Matter.Driver\P3.Driver.Matter.Driver.csproj",
    "automatica.driver.modbus\P3.Driver.ModBusDriverFactory\P3.Driver.ModBusDriverFactory.csproj",
    "automatica.driver.wakeonlan\P3.Driver.WakeOnLan\P3.Driver.WakeOnLan.csproj",
    "automatica.driver.ping\P3.Driver.PingFactory\P3.Driver.PingFactory.csproj",
    "automatica.driver.openweathermap.driver\P3.Driver.OpenWeatherMap.Driver\P3.Driver.OpenWeatherMap.DriverFactory.csproj"
)

# logics (relative to src/automatica.logics), all but EnOcean (hardware specific)
$logics = @(
    "automatica.logic.compare.baseoperations\P3.Logic.Compare.BaseOperations\P3.Logic.Compare.BaseOperations.csproj",
    "automatica.logic.digitaltoanalog\P3.Logic.DigitalToAnalog\P3.Logic.DigitalToAnalog.csproj",
    "automatica.logic.lightning\P3.Logic.Lightning\P3.Logic.Lightning.csproj",
    "automatica.logic.logic.baseoperations\P3.Logic.Logic.BaseOperations\P3.Logic.Logic.BaseOperations.csproj",
    "automatica.logic.math.basicoperations\P3.Logic.Math.BasicOperations\P3.Logic.Math.BasicOperations.csproj",
    "automatica.logic.messenger\P3.Logic.Messenger\P3.Logic.MessengerFactory\P3.Logic.Messenger.csproj",
    "automatica.logic.metering\Automatica.Logic.Metering\Automatica.Logic.Metering\Automatica.Logic.Metering.csproj",
    "automatica.logic.operations\P3.Logic.Operations\P3.Logic.Operations.csproj",
    "automatica.logic.string.baseoperations\P3.Logic.StringOperations\P3.Logic.StringOperations.csproj",
    "automatica.logic.surveillance\P3.Logic.Surveillance\P3.Logic.Surveillance.csproj",
    "automatica.logic.texttospeech\Automatica.Logic.TextToSpeech\Automatica.Logic.TextToSpeech\Automatica.Logic.TextToSpeech.csproj",
    "automatica.logic.time\P3.Logic.Time\P3.Logic.Time.csproj",
    # the media player visu element (no Sonos device needed to show it)
    "automatica.sonos.control\P3.Logic.Sonos\P3.Logic.Sonos.csproj"
)

# the libraries the plugins reference as packages
$corePackages = @(
    "Automatica.Core.Model\Automatica.Core.Model.csproj",
    "Automatica.Core.Model.Standard\Automatica.Core.Model.Standard.csproj",
    "Automatica.Core.EF\Automatica.Core.EF.csproj",
    "Automatica.Core.Base\Automatica.Core.Base.csproj",
    "Automatica.Core.Control\Automatica.Core.Control.csproj",
    "Automatica.Core.Notification\Automatica.Core.Notification.csproj",
    "Automatica.Core.Common.Update\Automatica.Core.Common.Update.csproj",
    "Automatica.Core.Driver\Automatica.Core.Driver.csproj",
    "Automatica.Core.Driver.Utility\Automatica.Core.Driver.Utility.csproj",
    "Automatica.Core.Rule\Automatica.Core.Logic.csproj"
)

function Invoke-Dotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE" }
}

function Get-ComponentName([string]$projectFile) {
    $manifest = Join-Path (Split-Path $projectFile) "automatica-manifest.json"
    return (Get-Content $manifest -Raw | ConvertFrom-Json).Automatica.ComponentName
}

New-Item -ItemType Directory -Force $buildDir | Out-Null
New-Item -ItemType Directory -Force $feedDir | Out-Null

# restore only from nuget.org and the local feed: the user's own NuGet sources (private feeds) are not needed
$nugetConfig = Join-Path $buildDir "NuGet.Config"
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="automatica-demo" value="$feedDir" />
  </packageSources>
</configuration>
"@ | Set-Content -Encoding utf8 $nugetConfig
$restoreConfig = "-p:RestoreConfigFile=$nugetConfig"

if (-not $SkipCore) {
    Write-Host "== packing Automatica.Core libraries $CoreVersion" -ForegroundColor Cyan
    Get-ChildItem $feedDir -Filter *.nupkg | Remove-Item -Force

    # the version is fixed, so a previous demo build may have left the same version in the global package cache
    $nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE ".nuget\packages" }
    foreach ($id in @("automatica.core.base", "automatica.core.common.update", "automatica.core.control", "automatica.core.driver",
            "automatica.core.driver.utility", "automatica.core.ef", "automatica.core.logic", "automatica.core.model",
            "automatica.core.model.standard", "automatica.core.notification")) {
        $cached = Join-Path $nugetRoot "$id\$CoreVersion"
        if (Test-Path $cached) { Remove-Item -Recurse -Force $cached }
    }

    foreach ($p in $corePackages) {
        Invoke-Dotnet pack (Join-Path $coreSrc $p) -c $Configuration -o $feedDir "-p:Version=$CoreVersion" "-p:GeneratePackageOnBuild=false" $restoreConfig --nologo "-v:minimal"
    }

    Write-Host "== publishing Automatica.Core (linux-x64)" -ForegroundColor Cyan
    if (Test-Path $appDir) { Remove-Item -Recurse -Force $appDir }
    Invoke-Dotnet publish (Join-Path $coreSrc "Automatica.Core\Automatica.Core.csproj") -c $Configuration -r linux-x64 --self-contained false `
        -o $appDir "-p:Version=$CoreVersion" "-p:GeneratePackageOnBuild=false" $restoreConfig --nologo "-v:minimal"
}

if (-not $SkipPlugins) {
    Write-Host "== publishing drivers and logics" -ForegroundColor Cyan
    if (Test-Path $pluginDir) { Remove-Item -Recurse -Force $pluginDir }

    # assemblies of the shared frameworks (Microsoft.NETCore.App, Microsoft.AspNetCore.App 10.0) of the local SDK
    $dotnetRoot = Split-Path (Get-Command dotnet).Source
    $frameworkFiles = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($fw in @("Microsoft.NETCore.App", "Microsoft.AspNetCore.App")) {
        $fwDir = Get-ChildItem (Join-Path $dotnetRoot "shared\$fw") -Directory -Filter "10.0.*" | Sort-Object Name | Select-Object -Last 1
        if ($fwDir) { Get-ChildItem $fwDir.FullName -File | ForEach-Object { [void]$frameworkFiles.Add($_.Name) } }
    }

    $jobs = @()
    foreach ($d in $drivers) { $jobs += [pscustomobject]@{ Project = Join-Path $repoRoot "src\automatica.drivers\$d"; Kind = "Drivers" } }
    foreach ($l in $logics) { $jobs += [pscustomobject]@{ Project = Join-Path $repoRoot "src\automatica.logics\$l"; Kind = "Rules" } }

    foreach ($job in $jobs) {
        $component = Get-ComponentName $job.Project
        $out = Join-Path $pluginDir "$($job.Kind)\$component"
        Write-Host "-- $($job.Kind)/$component" -ForegroundColor DarkCyan
        Invoke-Dotnet publish $job.Project -c $Configuration -o $out --nologo "-v:minimal" $restoreConfig `
            "-p:CustomBeforeMicrosoftCommonTargets=$pluginTargets" "-p:AutomaticaDemoCoreVersion=$CoreVersion"

        # the core loads plugins into its own load context, so every assembly the core ships is taken from the core:
        # the copies in the plugin folder are never used (and every file is probed for a manifest at startup)
        # (the same goes for the shared framework). Native libraries for linux-x64 are moved next to the plugin assemblies.
        $output = (Get-Content (Join-Path (Split-Path $job.Project) "automatica-manifest.json") -Raw | ConvertFrom-Json).Automatica.Output
        $native = Join-Path $out "runtimes\linux-x64\native"
        if (Test-Path $native) { Copy-Item (Join-Path $native "*") $out -Force }
        Get-ChildItem $out -Directory | Remove-Item -Recurse -Force
        Get-ChildItem $out -File | Where-Object {
            $_.Name -ne $output -and ((Test-Path (Join-Path $appDir $_.Name)) -or $frameworkFiles.Contains($_.Name))
        } | Remove-Item -Force
    }
}

if (-not $SkipFrontend) {
    Write-Host "== building the frontend" -ForegroundColor Cyan
    $web = Join-Path $coreSrc "Automatica.WebNew"
    if (-not (Test-Path (Join-Path $web "node_modules"))) { throw "Automatica.WebNew/node_modules is missing (npm ci)" }
    if (-not (Test-Path (Join-Path $web "src\devextreme-license.ts"))) { throw "Automatica.WebNew/src/devextreme-license.ts is missing" }
    Push-Location $web
    try {
        # not 'npm run build-docker': its pre script regenerates the license file
        & npx ng build --configuration=production,dist
        if ($LASTEXITCODE -ne 0) { throw "ng build failed" }
    }
    finally { Pop-Location }

    $wwwroot = Join-Path $buildDir "wwwroot"
    if (Test-Path $wwwroot) { Remove-Item -Recurse -Force $wwwroot }
    Copy-Item -Recurse (Join-Path $web "dist") $wwwroot
}

if (-not $SkipImage) {
    Write-Host "== docker build $Tag" -ForegroundColor Cyan
    & docker build -f (Join-Path $demoDir "Dockerfile.local") -t $Tag $demoDir
    if ($LASTEXITCODE -ne 0) { throw "docker build failed" }
}

Write-Host "done: docker run -d --name automatica-demo -p 5001:5001 $Tag" -ForegroundColor Green
