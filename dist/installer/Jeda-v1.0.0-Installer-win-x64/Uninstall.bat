@echo off
title JEDA Uninstaller
cls
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-Jeda.ps1"
echo.
echo Selesai.
timeout /t 3 >nul

