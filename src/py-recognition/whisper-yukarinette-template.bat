@echo off
pushd "%~dp0"
.\dist\recognize\recognize.exe \
  --out yukarinette \
  --method faster_whisper \
  --whisper_model medium \
  --whisper_language ja \
  --out_yukarinette 49513 
pause