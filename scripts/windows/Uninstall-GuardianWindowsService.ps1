#Requires -Version 5.1
#Requires -RunAsAdministrator
param([string]$ServiceName = 'ParentSiteGuardianApi')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $existing) {
    Write-Host "No service named $ServiceName."
    exit 0
}

if ($existing.Status -eq 'Running') {
    Stop-Service -Name $ServiceName -Force
}

& sc.exe delete $ServiceName
if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 1060) {
    throw "sc.exe delete failed with exit $LASTEXITCODE"
}
Write-Host "Removed service: $ServiceName"
