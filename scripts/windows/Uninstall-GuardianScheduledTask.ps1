#Requires -Version 5.1
param([string]$TaskName = 'ParentSiteGuardianApi')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$exists = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if (-not $exists) {
    Write-Host "No task named $TaskName."
    exit 0
}

try {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
}
catch {
    Write-Host "Unregister failed (try elevated PowerShell if this task was created as SYSTEM): $_"
    exit 1
}
Write-Host "Removed scheduled task: $TaskName"
