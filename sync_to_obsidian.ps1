# ==============================================================================
# DENTIA -> OBSIDIAN AUTO-SYNC SCRIPT
# ==============================================================================
# This script synchronizes all project documentation, clinical specialty guides,
# database migrations, and the visual architecture canvas from DentistApp_Theme2
# to your active Obsidian Vault at: F:\Obsedian\dentistAPP\dentistAPP
# ==============================================================================

$SourceDir = "F:\DentistApp_Theme2"
$ObsidianVault = "F:\Obsedian\dentistAPP\dentistAPP"
$ObsidianConfig = "F:\Obsedian\dentistAPP\.obsidian"

if (-not (Test-Path $ObsidianVault)) {
    New-Item -ItemType Directory -Path $ObsidianVault -Force | Out-Null
}

Write-Host "[*] Syncing Dentia Documentation to Obsidian Vault..." -ForegroundColor Cyan

$synced = 0
Get-ChildItem -Path $SourceDir -File | Where-Object {
    $_.Extension -in @(".md", ".canvas", ".sql", ".txt") -and -not $_.Name.StartsWith("~")
} | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination (Join-Path $ObsidianVault $_.Name) -Force
    $synced++
}

# Sync Obsidian configurations
if (Test-Path "$SourceDir\.obsidian") {
    @("graph.json", "app.json", "core-plugins.json") | ForEach-Object {
        $cfg = Join-Path "$SourceDir\.obsidian" $_
        if (Test-Path $cfg) {
            Copy-Item -Path $cfg -Destination (Join-Path $ObsidianConfig $_) -Force
        }
    }
}

Write-Host "[OK] Successfully synced $synced documentation, canvas, and schema files into Obsidian!" -ForegroundColor Green
Write-Host "     Vault Path: $ObsidianVault" -ForegroundColor Gray
