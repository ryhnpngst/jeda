# JEDA Installer Script v1.0.0
$ErrorActionPreference = "Stop"

$appSource = Join-Path $PSScriptRoot "app"
$targetDir = Join-Path $env:LOCALAPPDATA "Programs\Jeda"
$exePath = Join-Path $targetDir "Jeda.exe"
$icoPath = Join-Path $targetDir "Assets\app.ico"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "        MEMASANG JEDA v1.0.0 (INSTALLER RESMI)           " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Pastikan proses JEDA lama ditutup
$running = Get-Process Jeda -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "[1/5] Menutup proses JEDA yang sedang berjalan..." -ForegroundColor Yellow
    $running | Stop-Process -Force
    Start-Sleep -Seconds 1
} else {
    Write-Host "[1/5] Menyiapkan direktori instalasi..." -ForegroundColor Green
}

# 2. Buat folder dan salin file
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}
Write-Host "[2/5] Menyalin berkas program ke $targetDir..." -ForegroundColor Green
Copy-Item -Path "$appSource\*" -Destination $targetDir -Recurse -Force

# Salin script uninstaller ke folder program
Copy-Item -Path (Join-Path $PSScriptRoot "Uninstall-Jeda.ps1") -Destination $targetDir -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Uninstall.bat") -Destination $targetDir -Force

# 3. Buat shortcut Desktop dan Start Menu menggunakan WScript.Shell
Write-Host "[3/5] Membuat pintasan (shortcuts)..." -ForegroundColor Green
$wsh = New-Object -ComObject WScript.Shell

# Desktop Shortcut
$desktopPath = [Environment]::GetFolderPath("Desktop")
$desktopShortcut = $wsh.CreateShortcut((Join-Path $desktopPath "JEDA.lnk"))
$desktopShortcut.TargetPath = $exePath
$desktopShortcut.WorkingDirectory = $targetDir
$desktopShortcut.IconLocation = "$icoPath,0"
$desktopShortcut.Description = "JEDA – Pause the Screen, Keep Working"
$desktopShortcut.Save()

# Start Menu Shortcut
$startMenuPath = [Environment]::GetFolderPath("Programs")
$startMenuShortcut = $wsh.CreateShortcut((Join-Path $startMenuPath "JEDA.lnk"))
$startMenuShortcut.TargetPath = $exePath
$startMenuShortcut.WorkingDirectory = $targetDir
$startMenuShortcut.IconLocation = "$icoPath,0"
$startMenuShortcut.Description = "JEDA – Desktop Presentation Utility"
$startMenuShortcut.Save()

# 4. Daftarkan di Windows Settings (Add/Remove Programs)
Write-Host "[4/5] Mendaftarkan informasi aplikasi ke sistem Windows..." -ForegroundColor Green
$regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Jeda"
if (-not (Test-Path $regPath)) {
    New-Item -Path $regPath -Force | Out-Null
}
Set-ItemProperty -Path $regPath -Name "DisplayName" -Value "JEDA – Desktop Presentation Utility"
Set-ItemProperty -Path $regPath -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $regPath -Name "Publisher" -Value "JEDA Open Source"
Set-ItemProperty -Path $regPath -Name "DisplayIcon" -Value "$icoPath,0"
Set-ItemProperty -Path $regPath -Name "InstallLocation" -Value $targetDir
Set-ItemProperty -Path $regPath -Name "UninstallString" -Value "`"$targetDir\Uninstall.bat`""

# 5. Selesai
Write-Host "[5/5] Instalasi berhasil diselesaikan!" -ForegroundColor Green
Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   JEDA v1.0.0 telah berhasil terpasang di komputer Anda! " -ForegroundColor Cyan
Write-Host "   Pintasan telah dibuat di Desktop dan Start Menu.       " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""

