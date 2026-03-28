@echo off
setlocal
start "" powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "%~dp0Start-Crosshair.ps1" -ConfigPath "%~dp0config\crosshair.json"
