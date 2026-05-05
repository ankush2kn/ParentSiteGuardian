#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Installs the published API as a Windows Service (SCM). Secrets stay local; no cloud.

.NOTES
  Registers the SCM binary path as dotnet.exe plus the published DLL (a .cmd file cannot be a Windows service entry point). Scheduled tasks may still use Run-GuardianService.cmd. Service runs as Local System unless you change it in services.msc.
#>
param(
    [string]$PublishDirectory = "",
    [string]$ServiceName = 'ParentSiteGuardianApi',
    [string]$DisplayName = 'Parent Site Guardian API',
    [string]$RepoRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\_RepoRoot.ps1"
if (-not $RepoRoot) { $RepoRoot = $script:GuardianRepoRoot }
if (-not $PublishDirectory) {
    $PublishDirectory = Join-Path $RepoRoot 'dist\install\parent-site-guardian-service'
}

$dll = Join-Path $PublishDirectory 'ParentSiteGuardian.Service.dll'
if (-not (Test-Path -LiteralPath $dll)) {
    throw "Published DLL not found: $dll`nRun scripts\windows\Build-Service.ps1 first."
}
$dotnetExe = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetExe)) {
    throw ".NET host not found at $dotnetExe (install the x64 .NET runtime or SDK to the default location)."
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
    }
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

$binaryPathName = "`"$dotnetExe`" `"$dll`""
New-Service -Name $ServiceName -BinaryPathName $binaryPathName -DisplayName $DisplayName -StartupType Automatic `
    -Description 'Local SQLite-backed API for Parent Site Guardian (127.0.0.1:5050).' | Out-Null

Write-Host "Created service: $ServiceName"
Write-Host "Starting..."
Start-Service -Name $ServiceName
Write-Host "Smoke test: Invoke-RestMethod http://127.0.0.1:5050/health"
