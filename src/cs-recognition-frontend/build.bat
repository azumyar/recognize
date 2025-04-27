@echo off
PATH=%PATH%;C:\Program Files\dotnet
pushd "%~dp0"

dotnet clean -c Release recognize-frontend.csproj
set r=%errorlevel%
if %r% neq 0 goto error

dotnet build -c Release recognize-frontend.csproj
set r=%errorlevel%
if %r% neq 0 goto error

dotnet publish -c Release recognize-frontend.csproj
set r=%errorlevel%
if %r% neq 0 goto error

popd

:error
exit /B %r%