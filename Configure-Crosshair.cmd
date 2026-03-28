@echo off
setlocal
start "" powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Configure-Crosshair.ps1" -ConfigPath "%~dp0config\crosshair.json"
