@echo off
pushd "%~dp0"
powershell "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process ; .\build.ps1"
pause