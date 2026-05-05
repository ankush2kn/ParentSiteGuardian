#Requires -Version 5.1
<#
.SYNOPSIS
  Copies the local SQLite database to a timestamped file under ProgramData backups.

.PARAMETER DatabasePath
  Full path to parent-site-guardian.db. If omitted, uses GuardianDatabase:Path env var or the default ProgramData location.
#>
param(
    [string]$DatabasePath = "",
    [string]$BackupDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-DefaultDatabasePath {
    foreach ($name in @('GUARDIAN_DATABASE_PATH', 'GuardianDatabase__Path')) {
        $fromEnv = [Environment]::GetEnvironmentVariable($name, 'Machine')
        if ([string]::IsNullOrWhiteSpace($fromEnv)) { $fromEnv = [Environment]::GetEnvironmentVariable($name, 'User') }
        if (-not [string]::IsNullOrWhiteSpace($fromEnv)) { return $fromEnv.Trim() }
    }
    return Join-Path $env:ProgramData 'ParentSiteGuardian\parent-site-guardian.db'
}

if ([string]::IsNullOrWhiteSpace($DatabasePath)) {
    $DatabasePath = Get-DefaultDatabasePath
}

if (-not (Test-Path -LiteralPath $DatabasePath)) {
    Write-Error "Database file not found: $DatabasePath (start the service once to create it, or pass -DatabasePath)."
}

if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
    $BackupDirectory = Join-Path $env:ProgramData 'ParentSiteGuardian\backups'
}
New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$leaf = [System.IO.Path]::GetFileNameWithoutExtension($DatabasePath)
$dest = Join-Path $BackupDirectory "$leaf-$stamp.db"

Copy-Item -LiteralPath $DatabasePath -Destination $dest -Force
Write-Host "Backed up to: $dest"
