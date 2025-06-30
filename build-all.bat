@echo off
pushd "%~dp0"

echo start build > build.log

set r=0
if exist "bin\tee.exe" (
  echo existed tee.exe>> build.log 
) else (
  echo create tee.exe >> build.log
  echo ビルドの初回準備を行います
  C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc /out:bin\tee.exe /target:exe /o+ src\tee\Program.cs 2>&1 >> build.log
  set r=%errorlevel%
  echo ok
  echo ""
)
if %r% neq 0 goto error

pushd src\py-recognition
(powershell "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process ; .\build.ps1" 2>&1 & call echo %%^^errorlevel%% ^> ..\..\build.dat)  | ..\..\bin\tee.exe --mask ..\..\build.log
popd
(
    set /P r=
)< build.dat
if %r% neq 0 goto error

(call src\cs-illuminate\build.bat 2>&1 & call echo %%^^errorlevel%% ^> build.dat) | bin\tee.exe --mask build.log
popd
(
    set /P r=
)< build.dat
(call src\cs-recognition-frontend\build.bat 2>&1 & call echo %%^^errorlevel%% ^> build.dat) | bin\tee.exe --mask build.log
popd
(
    set /P r=
)< build.dat
if %r% neq 0 goto error
(copy src\cs-recognition-frontend\bin\Release\net8.0-windows\win-x64\publish\recognize-gui.exe .\ 2>&1 & call echo %%^^errorlevel%% ^> build.dat) | bin\tee.exe --mask build.log
popd
(
    set /P r=
)< build.dat
if %r% neq 0 goto error

echo build sucess >> build.log
echo ######################################################
echo #
echo # ビルドが成功しました！
echo #   recognize-gui.exeを起動してください
echo #
echo ######################################################

:end
pause
exit

:error
echo build error >> build.log
echo ######################################################
echo #
echo # ビルドが失敗しました
echo #   build.log にビルド情報が記載されているのでそれを
echo #   連携いただくと解決がスムーズかもしれません
echo # ！！個人情報が記載されている可能性がります！！
echo # ！！必ず事前にご確認してください！！
echo #
echo ######################################################

pause