#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the React admin UI to apps\admin-ui\dist (Vite production build).
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
    Write-Host "Building admin-ui..."
    & npm run build:admin
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

$dist = Join-Path $RepoRoot 'apps\admin-ui\dist'
Write-Host "Admin UI output: $dist"
Write-Host "Preview locally: npm run preview -w admin-ui   (from repo root)"
