# ============================================================
# build-installer.ps1
# Publishes the app and packages it with Inno Setup.
# Run from the repo root:  .\installer\build-installer.ps1
# ============================================================

$ErrorActionPreference = "Stop"
$root    = Split-Path $PSScriptRoot -Parent
$distDir = Join-Path $root "dist\AlindaBrend"
$outDir  = Join-Path $root "dist\Installer"
$proj    = Join-Path $root "src\BusinessManager.App\BusinessManager.App.csproj"
$iss     = Join-Path $root "installer\AlindaBrend.iss"

# ── 1. Clean previous output ────────────────────────────────
Write-Host "`n[1/3] Cleaning dist..." -ForegroundColor Cyan
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $distDir | Out-Null
New-Item -ItemType Directory -Force -Path $outDir  | Out-Null

# ── 2. Publish self-contained release build ─────────────────
Write-Host "[2/3] Publishing (self-contained, win-x64)..." -ForegroundColor Cyan
dotnet publish $proj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    --output $distDir

if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit 1 }
Write-Host "      Published to: $distDir" -ForegroundColor Green

# ── 3. Build installer with Inno Setup ──────────────────────
Write-Host "[3/3] Building installer..." -ForegroundColor Cyan

$isccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
)
$iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host ""
    Write-Host "  Inno Setup not found on this machine." -ForegroundColor Yellow
    Write-Host "  Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "  After installing, re-run this script to produce the .exe installer." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  The published app files are ready in:" -ForegroundColor Green
    Write-Host "  $distDir" -ForegroundColor Green
    exit 0
}

& $iscc $iss
if ($LASTEXITCODE -ne 0) { Write-Error "Inno Setup build failed"; exit 1 }

$installer = Get-ChildItem $outDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host ""
Write-Host "  Installer ready: $($installer.FullName)" -ForegroundColor Green
Write-Host "  Size: $([math]::Round($installer.Length / 1MB, 1)) MB" -ForegroundColor Green
