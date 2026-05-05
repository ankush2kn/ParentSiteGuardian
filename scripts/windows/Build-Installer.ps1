#Requires -Version 5.1
<#
.SYNOPSIS
  Builds self-contained service + admin UI + extension, stages files under installer/staging, then compiles the Inno Setup installer when ISCC is available.

.PARAMETER SkipInno
  Only populate installer/staging (no ISCC.exe required).

.PARAMETER SkipTests
  Skip dotnet test before publishing (faster local iteration).
#>
param(
    [switch]$SkipInno,
    [switch]$SkipTests,
    [string]$RepoRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\_RepoRoot.ps1"
if (-not $RepoRoot) { $RepoRoot = $script:GuardianRepoRoot }

$installerDir = Join-Path $RepoRoot 'installer'
$staging = Join-Path $installerDir 'staging'
$iss = Join-Path $installerDir 'ParentSiteGuardian.iss'

if (-not (Test-Path -LiteralPath $iss)) { throw "Missing Inno script: $iss" }

Write-Host "Cleaning staging: $staging"
Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $staging -Force | Out-Null

Push-Location $RepoRoot
try {
    Write-Host "npm install..."
    & npm install
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (-not $SkipTests) {
        Write-Host "dotnet test..."
        & dotnet test (Join-Path $RepoRoot 'apps\parent-site-guardian-service\tests\ParentSiteGuardian.Service.Tests\ParentSiteGuardian.Service.Tests.csproj') -c Release
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    Write-Host "Building admin UI + extension..."
    & npm run build:admin
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & npm run build:extension
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $serviceOut = Join-Path $staging 'service'
    Write-Host "dotnet publish (self-contained win-x64) -> $serviceOut"
    $proj = Join-Path $RepoRoot 'apps\parent-site-guardian-service\ParentSiteGuardian.Service.csproj'
    & dotnet publish $proj -c Release -r win-x64 --self-contained true -o $serviceOut
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $adminSrc = Join-Path $RepoRoot 'apps\admin-ui\dist'
    $extSrc = Join-Path $RepoRoot 'apps\browser-extension\dist'
    if (-not (Test-Path -LiteralPath $adminSrc)) { throw "Missing $adminSrc. Run build:admin first." }
    if (-not (Test-Path -LiteralPath $extSrc)) { throw "Missing $extSrc. Run build:extension first." }

    $adminDst = Join-Path $staging 'admin-ui'
    $extDst = Join-Path $staging 'extension'
    New-Item -ItemType Directory -Path $adminDst -Force | Out-Null
    Copy-Item -Path (Join-Path $adminSrc '*') -Destination $adminDst -Recurse -Force
    New-Item -ItemType Directory -Path $extDst -Force | Out-Null
    Copy-Item -Path (Join-Path $extSrc '*') -Destination $extDst -Recurse -Force
}
finally {
    Pop-Location
}

Write-Host "Staging complete: $staging"

if ($SkipInno) {
    Write-Host "SkipInno: not invoking ISCC."
    exit 0
}

$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
)
$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Warning "Inno Setup 6 (ISCC.exe) not found. Install from https://jrsoftware.org/isdl.php then re-run, or use -SkipInno."
    Write-Host "Output would be: $(Join-Path $RepoRoot 'dist\installer\ParentSiteGuardian-Setup-0.1.0.exe')"
    exit 0
}

Write-Host "Compiling installer with: $iscc"
& $iscc $iss
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$outExe = Join-Path $RepoRoot 'dist\installer\ParentSiteGuardian-Setup-0.1.0.exe'
Write-Host "Installer: $outExe"
