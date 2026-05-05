#Requires -Version 5.1
<#
.SYNOPSIS
  Production-builds the MV3 browser extension to apps\browser-extension\dist.
#>
param([string]$RepoRoot = "")

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\_RepoRoot.ps1"
if (-not $RepoRoot) { $RepoRoot = $script:GuardianRepoRoot }

Push-Location $RepoRoot
try {
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot 'node_modules'))) {
        Write-Host "Running npm install at repo root..."
        & npm install
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    Write-Host "Building browser extension..."
    & npm run build:extension
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

$dist = Join-Path $RepoRoot 'apps\browser-extension\dist'
Write-Host "Extension output: $dist"
Write-Host "Load unpacked in Chrome/Edge: point to this folder (see docs\windows-phase1-install.md)."
