#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes the .NET guardian API to a local folder (no cloud).

.PARAMETER OutputDirectory
  Defaults to dist\install\parent-site-guardian-service under the repo root.
#>
param(
    [string]$RepoRoot = "",
    [string]$OutputDirectory = "",
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\_RepoRoot.ps1"
if (-not $RepoRoot) { $RepoRoot = $script:GuardianRepoRoot }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $RepoRoot 'dist\install\parent-site-guardian-service' }

$proj = Join-Path $RepoRoot 'apps\parent-site-guardian-service\ParentSiteGuardian.Service.csproj'
if (-not (Test-Path -LiteralPath $proj)) { throw "Project not found: $proj" }

Write-Host "Publishing service to: $OutputDirectory"
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
& dotnet publish $proj -c $Configuration -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Launcher for Task Scheduler / manual runs; uses default x64 SDK path so LocalSystem finds dotnet.
$cmdPath = Join-Path $OutputDirectory 'Run-GuardianService.cmd'
$cmdLines = @(
    '@echo off',
    'set ASPNETCORE_ENVIRONMENT=Production',
    'cd /d "%~dp0"',
    'if exist "%ProgramFiles%\dotnet\dotnet.exe" (',
    '  "%ProgramFiles%\dotnet\dotnet.exe" "%~dp0ParentSiteGuardian.Service.dll"',
    ') else (',
    '  dotnet "%~dp0ParentSiteGuardian.Service.dll"',
    ')',
    'exit /b %ERRORLEVEL%'
)
Set-Content -LiteralPath $cmdPath -Value ($cmdLines -join "`r`n") -Encoding ascii
Write-Host "Wrote launcher: $cmdPath"
Write-Host "Smoke test (with service stopped): Run-GuardianService.cmd then GET http://127.0.0.1:5050/health"
