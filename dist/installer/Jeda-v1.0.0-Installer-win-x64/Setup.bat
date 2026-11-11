@echo off
title JEDA Installer v1.0.0
cls
echo ==========================================================
echo        MEMASANG JEDA v1.0.0 (INSTALLER SETUP)
echo ==========================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Jeda.ps1"
echo.
echo Tekan tombol apa saja untuk keluar dari pemasang...
pause >nul

