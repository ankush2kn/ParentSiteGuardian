#Requires -Version 5.1
<#
.SYNOPSIS
  Registers a scheduled task to start the published guardian API (no cloud).

.PARAMETER Trigger
  AtStartup = machine boot, runs as SYSTEM (recommended for shared family PC). Requires elevated PowerShell.
  AtLogOn = current Windows user logon (no SYSTEM; good for a single-user laptop). Can be registered without elevation.

.PARAMETER PublishDirectory
  Folder containing Run-GuardianService.cmd and ParentSiteGuardian.Service.dll (see Build-Service.ps1).
#>
param(
    [ValidateSet('AtStartup', 'AtLogOn')]
    [string]$Trigger = 'AtStartup',
    [string]$PublishDirectory = "",
    [string]$TaskName = 'ParentSiteGuardianApi',
    [string]$RepoRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\_RepoRoot.ps1"
if (-not $RepoRoot) { $RepoRoot = $script:GuardianRepoRoot }
if (-not $PublishDirectory) {
    $PublishDirectory = Join-Path $RepoRoot 'dist\install\parent-site-guardian-service'
}

$launcher = Join-Path $PublishDirectory 'Run-GuardianService.cmd'
if (-not (Test-Path -LiteralPath $launcher)) {
    throw "Launcher not found: $launcher`nRun scripts\windows\Build-Service.ps1 first (or pass -PublishDirectory)."
}

function Test-IsAdministrator {
    $p = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if ($Trigger -eq 'AtStartup' -and -not (Test-IsAdministrator)) {
    throw "Trigger AtStartup must be registered from an elevated PowerShell (Run as administrator)."
}

$exists = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($exists) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}

$action = New-ScheduledTaskAction -Execute $launcher -WorkingDirectory $PublishDirectory

if ($Trigger -eq 'AtStartup') {
    $t = New-ScheduledTaskTrigger -AtStartup
    $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $t -Principal $principal -Description 'Parent Site Guardian local API (http://127.0.0.1:5050)' | Out-Null
}
else {
    $t = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
    $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Limited
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $t -Principal $principal -Description 'Parent Site Guardian local API (http://127.0.0.1:5050)' | Out-Null
}

Write-Host "Registered scheduled task: $TaskName ($Trigger)"
Write-Host "Test: Start-ScheduledTask -TaskName '$TaskName' then open http://127.0.0.1:5050/health"
