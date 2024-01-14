@echo off
pushd "%~dp0"
.\dist\recognize\recognize.exe ^
  --out yukacone ^
  --method google ^
  --disable_lpf ^
  --disable_hpf  ^
  --out_yukacone 49513 
pause