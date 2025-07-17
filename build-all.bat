@echo off
pushd "%~dp0"

echo ############## recognize をビルドします ##############
pushd src\py-recognition
powershell "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process ; .\build.ps1"
if %errorlevel% neq 0 goto error
popd

echo ############ recognize-gui をビルドします ############
call src\cs-recognition-frontend\build.bat
if %errorlevel% neq 0 goto error
copy src\cs-recognition-frontend\dist\recognize-gui.exe 
if %errorlevel% neq 0 goto error


echo ############# illuminate をビルドします ##############
call src\cs-illuminate\build.bat 
if %errorlevel% neq 0 goto error

echo ######################################################
echo #
echo # ビルドが成功しました！
echo #   recognize-gui.exeを起動してください
echo #
echo ######################################################

:end
pause
exit /B

:error
echo ######################################################
echo #
echo # ビルドが失敗しました
echo #
echo ######################################################

pause
exit /B 1