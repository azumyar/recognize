@echo off
pushd "%~dp0"

C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc /out:batmake.exe /target:winexe /o+ src\*.cs
pause