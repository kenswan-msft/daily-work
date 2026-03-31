#Requires -Version 5.1
<#
.SYNOPSIS
    DailyWork CLI — Install / Update Script
.DESCRIPTION
    Packs the CLI as a global .NET tool and writes ~/.dailywork/config.json
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$CliProject = Join-Path $RepoRoot 'src' 'DailyWork.Cli' 'DailyWork.Cli.csproj'
$AppHostPath = Join-Path $RepoRoot 'src' 'DailyWork.AppHost'
$NupkgDir = Join-Path $RepoRoot 'src' 'DailyWork.Cli' 'nupkg'
$ConfigDir = Join-Path $HOME '.dailywork'
$ConfigFile = Join-Path $ConfigDir 'config.json'
$PackageId = 'DailyWork.Cli'
$ToolName = 'daily'

Write-Host ''
Write-Host '╔══════════════════════════════════════════╗' -ForegroundColor Cyan
Write-Host '║   DailyWork CLI — Install / Update       ║' -ForegroundColor Cyan
Write-Host '╚══════════════════════════════════════════╝' -ForegroundColor Cyan
Write-Host ''

# Verify prerequisites
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet SDK is not installed or not in PATH.'
    exit 1
}

if (-not (Test-Path $CliProject)) {
    Write-Error "CLI project not found at $CliProject"
    exit 1
}

# Clean previous packages
Write-Host '→ Cleaning previous packages...'
if (Test-Path $NupkgDir) {
    Remove-Item -Recurse -Force $NupkgDir
}

# Pack the CLI project
Write-Host '→ Packing DailyWork.Cli...'
dotnet pack $CliProject -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Install or update the global tool
Write-Host "→ Installing global tool '$ToolName'..."
$installedTools = dotnet tool list -g
if ($installedTools -match $PackageId) {
    dotnet tool update --global --add-source $NupkgDir $PackageId --no-cache
} else {
    dotnet tool install --global --add-source $NupkgDir $PackageId --no-cache
}
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Write configuration file
Write-Host "→ Writing configuration to $ConfigFile..."
if (-not (Test-Path $ConfigDir)) {
    New-Item -ItemType Directory -Path $ConfigDir -Force | Out-Null
}

$config = @{
    AppHostProjectPath = $AppHostPath
    DailyWorkApiOptions = @{
        BaseAddress  = 'https://localhost:7048'
        ChatEndpoint = '/api/chat'
    }
} | ConvertTo-Json -Depth 3

Set-Content -Path $ConfigFile -Value $config -Encoding UTF8

Write-Host ''
Write-Host '✓ DailyWork CLI installed successfully!' -ForegroundColor Green
Write-Host ''
Write-Host "  Tool command:   $ToolName"
Write-Host "  AppHost path:   $AppHostPath"
Write-Host "  Config file:    $ConfigFile"
Write-Host ''
Write-Host "Run '$ToolName' from any directory to start."
