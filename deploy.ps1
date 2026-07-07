param(
    [string]$BlishExe = $env:GW2_BLISH_EXE
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot

Push-Location $projectRoot
try {
    dotnet build Gw2EventTracker.csproj -c Debug -p:Platform=x64
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
} finally {
    Pop-Location
}

$modulesDir = Join-Path $env:USERPROFILE "Documents\Guild Wars 2\addons\blishhud\modules"
$oneDriveModules = Join-Path $env:OneDrive "Documents\Guild Wars 2\addons\blishhud\modules"
if ($env:OneDrive -and (Test-Path (Split-Path $oneDriveModules -Parent))) {
    $modulesDir = $oneDriveModules
}

New-Item -ItemType Directory -Force -Path $modulesDir | Out-Null

$bhm = Join-Path $projectRoot "bin\x64\Debug\Gw2EventTracker.bhm"
$dest = Join-Path $modulesDir "ghost.gw2eventtracker_0.1.2.bhm"

$blish = Get-Process -Name "Blish HUD" -ErrorAction SilentlyContinue
if ($blish) {
    Write-Host "Stopping Blish HUD..."
    $blish | Stop-Process -Force
    Start-Sleep -Seconds 2
}

Copy-Item $bhm $dest -Force
Write-Host "Deployed to $dest"

if ($BlishExe -and (Test-Path $BlishExe)) {
    Start-Process $BlishExe
    Write-Host "Restarted Blish HUD"
} elseif ($BlishExe) {
    Write-Host "Blish HUD not found at $BlishExe - start it manually to load the module"
} else {
    Write-Host "Set GW2_BLISH_EXE to your Blish HUD.exe path to auto-restart after deploy."
}
