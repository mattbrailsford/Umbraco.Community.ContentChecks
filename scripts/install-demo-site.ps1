#!/usr/bin/env pwsh
# Demo Site Setup Script (PowerShell)
# Generates a disposable Umbraco demo site (in the gitignored demos/ folder) that
# project-references the local Umbraco.Community.ContentChecks.Core, so the
# ContentChecksComposer auto-registers Content Checks. Mirrors the Umbraco.AI demo-site approach.

param(
    [switch]$SkipTemplateInstall,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir
Set-Location $RepoRoot

Write-Host "========================================="
Write-Host "Umbraco.Community.ContentChecks Demo Site Setup"
Write-Host "========================================="

$packagesProps = Join-Path $RepoRoot 'Directory.Packages.props'
$match = Select-String -Path $packagesProps -Pattern 'Include="Umbraco\.Cms" Version="\[([^,]+),' | Select-Object -First 1
if (-not $match) { Write-Error "Could not find Umbraco.Cms version range in $packagesProps"; exit 1 }
$templateVersion = $match.Matches[0].Groups[1].Value
$versionMajor = $templateVersion.Split('.')[0]
Write-Host "Target Umbraco.Cms template version: $templateVersion (v$versionMajor)"

$demoDir     = "demos/v$versionMajor"
$demoSiteDir = "$demoDir/Umbraco.Community.ContentChecks.DemoSite"
$localSln    = "Umbraco.Community.ContentChecks.local.slnx"

if ((Test-Path $demoDir) -and -not $Force) {
    Write-Host "Demo folder '$demoDir' already exists. Use -Force to recreate."
    exit 0
}
if ($Force) {
    if (Test-Path $demoDir) { Remove-Item -Recurse -Force $demoDir }
    if (Test-Path $localSln) { Remove-Item -Force $localSln }
}

$cleanVersion = switch ($versionMajor) { '17' { '7.*' } '18' { '8.*' } default { '' } }

if (-not $SkipTemplateInstall) {
    Write-Host "Installing Umbraco.Templates ($templateVersion)..."
    dotnet new uninstall Umbraco.Templates 2>$null | Out-Null
    dotnet new install "Umbraco.Templates::$templateVersion" --force
}

Write-Host "Creating demo folder '$demoDir'..."
New-Item -ItemType Directory -Force -Path $demoDir | Out-Null
Copy-Item "$ScriptDir/templates/Directory.Build.props" "$demoDir/Directory.Build.props"
Copy-Item "$ScriptDir/templates/Directory.Packages.props" "$demoDir/Directory.Packages.props"

Write-Host "Creating Umbraco demo site..."
Push-Location $demoDir
dotnet new umbraco --force -n "Umbraco.Community.ContentChecks.DemoSite" `
    --friendly-name "Administrator" --email "admin@example.com" --password "password1234" `
    --development-database-type SQLite
Pop-Location

New-Item -ItemType Directory -Force -Path "$demoSiteDir/Properties" | Out-Null
Copy-Item "$ScriptDir/templates/launchSettings.json" "$demoSiteDir/Properties/launchSettings.json"

Push-Location $demoSiteDir
if ($cleanVersion) {
    Write-Host "Installing Clean starter kit ($cleanVersion)..."
    dotnet add package Clean --version $cleanVersion
} else {
    Write-Host "Warning: no Clean version mapping for v$versionMajor; installing latest stable."
    dotnet add package Clean
}
Pop-Location

Write-Host "Adding project reference to Umbraco.Community.ContentChecks.Core..."
dotnet add "$demoSiteDir/Umbraco.Community.ContentChecks.DemoSite.csproj" reference `
    "src/Umbraco.Community.ContentChecks.Core/Umbraco.Community.ContentChecks.Core.csproj"

Write-Host "Creating local solution '$localSln'..."
dotnet new sln -n "Umbraco.Community.ContentChecks.local" --format slnx --force | Out-Null
dotnet sln $localSln add `
    "src/Umbraco.Community.ContentChecks.Core/Umbraco.Community.ContentChecks.Core.csproj" `
    "tests/Umbraco.Test.ContentChecks.Unit/Umbraco.Test.ContentChecks.Unit.csproj" `
    "tests/Umbraco.Test.ContentChecks.Integration/Umbraco.Test.ContentChecks.Integration.csproj" `
    "$demoSiteDir/Umbraco.Community.ContentChecks.DemoSite.csproj" | Out-Null

Write-Host ""
Write-Host "Setup Complete! Solution: $localSln  |  Demo site: $demoSiteDir  |  admin@example.com / password1234"
