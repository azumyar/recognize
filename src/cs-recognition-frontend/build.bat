@echo off
pushd "%~dp0"

C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc /out:recognize-gui.exe /target:winexe /o+ /win32icon:App.ico src\*.cs
set r=%errorlevel%
popd

exit /B %r%
