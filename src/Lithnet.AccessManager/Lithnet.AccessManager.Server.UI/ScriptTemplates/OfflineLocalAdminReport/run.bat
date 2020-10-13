@echo off
copy /Y "%~dp0\Get-LocalAdmins.ps1" "%temp%\Get-LocalAdmins.ps1"

PowerShell.exe -NoProfile -ExecutionPolicy Bypass -File "%temp%\Get-LocalAdmins.ps1" "%~dp0\.."

del /Q "%temp%\Get-LocalAdmins.ps1"