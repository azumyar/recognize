@echo off
pushd "%~dp0"
.\dist\recognize\recognize.exe ^
  --out yukacone ^
  --method google ^
  --disable_lpf ^
  --disable_hpf
pause