@echo off
pushd "%~dp0"

pushd py-recognition
powershell "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process ; .\build.ps1"
set r=%errorlevel% 
popd
if %r% neq 0 goto end

call cs-recognition-frontend\build.bat
if %errorlevel% neq 0 goto end
copy cs-recognition-frontend\recognize-gui.exe .\

echo ######################################################
echo # 
echo # ビルドが成功しました！
echo #   recognize-gui.exeを起動してください
echo # 
echo ######################################################

:end
pause