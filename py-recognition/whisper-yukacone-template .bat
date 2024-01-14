@echo off
pushd "%~dp0"
.\dist\recognize\recognize.exe ^
  --out yukacone ^
  --method faster_whisper ^
  --whisper_model medium ^
  --whisper_language ja ^
  --out_yukacone 49513 
pause