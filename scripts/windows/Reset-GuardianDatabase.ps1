#Requires -Version 5.1
<#
.SYNOPSIS
  Backs up then deletes the local SQLite database so the next service start recreates schema and seed.

.NOTES
  Stop the guardian API (and close admin UI if it locks the file) before running. Does not run dotnet ef or seed commands for you.
#>
param(
    [string]$DatabasePath = "",
    [switch]$SkipBackup,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $Force) {
    Write-Host "This will delete the local guardian database after an optional backup."
    $confirm = Read-Host "Type RESET to continue"
    if ($confirm -ne 'RESET') { Write-Host "Aborted."; exit 1 }
}

if ([string]::IsNullOrWhiteSpace($DatabasePath)) {
    $resolved = $null
    foreach ($name in @('GUARDIAN_DATABASE_PATH', 'GuardianDatabase__Path')) {
        $fromEnv = [Environment]::GetEnvironmentVariable($name, 'Machine')
        if ([string]::IsNullOrWhiteSpace($fromEnv)) { $fromEnv = [Environment]::GetEnvironmentVariable($name, 'User') }
        if (-not [string]::IsNullOrWhiteSpace($fromEnv)) { $resolved = $fromEnv.Trim(); break }
    }
    if ($resolved) { $DatabasePath = $resolved }
    else { $DatabasePath = Join-Path $env:ProgramData 'ParentSiteGuardian\parent-site-guardian.db' }
}

if (-not (Test-Path -LiteralPath $DatabasePath)) {
    Write-Host "Nothing to delete: $DatabasePath"
    exit 0
}

if (-not $SkipBackup) {
    & "$PSScriptRoot\Backup-GuardianDatabase.ps1" -DatabasePath $DatabasePath
}

try {
    Remove-Item -LiteralPath $DatabasePath -Force
    Write-Host "Removed: $DatabasePath"
}
catch {
    Write-Error "Could not delete the database (is the service still running?). $_"
}

Write-Host "Start the service again to apply migrations and first-run seed."
