@echo off
pushd "%~dp0"
.\dist\recognize\recognize.exe ^
  --out yukarinette ^
  --method google ^
  --disable_lpf ^
  --disable_hpf  ^
  --out_yukarinette 49513 
pause