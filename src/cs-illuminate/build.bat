@echo off
PATH=%PATH%;C:\Program Files\dotnet
pushd "%~dp0"

@rem Windows11 24H2 Sandboxで動かなかったので対策処理
if exist "C:\Users\WDAGUtilityAccount" if not exist "nuget.config" (
  pushd test
  dotnet new nugetconfig
  dotnet restore
  popd

  pushd VoiceLink
  dotnet new nugetconfig
  dotnet restore
  popd
)


pushd illuminate
dotnet clean -c Release
set r=%errorlevel%
if %r% neq 0 goto error

dotnet build -c Release
set r=%errorlevel%
if %r% neq 0 goto error

dotnet publish -c Release
set r=%errorlevel%
if %r% neq 0 goto error
popd

:error
exit /B %r%