#Requires -Version 5.1
<#
.SYNOPSIS
  Builds service (publish), admin UI, and browser extension — all local artifacts.
#>
param(
    [string]$RepoRoot = "",
    [string]$ServiceOutputDirectory = "",
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\_RepoRoot.ps1"
if (-not $RepoRoot) { $RepoRoot = $script:GuardianRepoRoot }

$serviceArgs = @{ RepoRoot = $RepoRoot; Configuration = $Configuration }
if ($ServiceOutputDirectory) { $serviceArgs.OutputDirectory = $ServiceOutputDirectory }
& "$PSScriptRoot\Build-Service.ps1" @serviceArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& "$PSScriptRoot\Build-Admin.ps1" -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& "$PSScriptRoot\Build-Extension.ps1" -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "All builds finished. Next: docs\windows-phase1-install.md (scheduled task, extension, database)."
