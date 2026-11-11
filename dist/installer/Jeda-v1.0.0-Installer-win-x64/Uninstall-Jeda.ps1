# JEDA Uninstaller Script v1.0.0
$ErrorActionPreference = "SilentlyContinue"

Write-Host "==========================================================" -ForegroundColor Yellow
Write-Host "                MENGHAPUS INSTALASI JEDA                  " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Yellow
Write-Host ""

# 1. Hentikan proses jika berjalan
$running = Get-Process Jeda -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Menutup proses JEDA..."
    $running | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# 2. Hapus Shortcut
Write-Host "Menghapus pintasan di Desktop dan Start Menu..."
$desktopPath = [Environment]::GetFolderPath("Desktop")
$startMenuPath = [Environment]::GetFolderPath("Programs")

Remove-Item -Force (Join-Path $desktopPath "JEDA.lnk")
Remove-Item -Force (Join-Path $startMenuPath "JEDA.lnk")

# 3. Hapus Registry
Write-Host "Menghapus pendaftaran di sistem Windows..."
Remove-Item -Recurse -Force "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Jeda"

# 4. Hapus folder instalasi
$targetDir = Join-Path $env:LOCALAPPDATA "Programs\Jeda"
Write-Host "Membersihkan direktori aplikasi..."
# Hapus semua berkas kecuali script ini sendiri
Get-ChildItem -Path $targetDir -Exclude "Uninstall-Jeda.ps1", "Uninstall.bat" | Remove-Item -Recurse -Force

Write-Host ""
Write-Host "JEDA telah berhasil dihapus sepenuhnya dari sistem Anda." -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Yellow

