@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0AspenBurner.Cli.ps1" %*
