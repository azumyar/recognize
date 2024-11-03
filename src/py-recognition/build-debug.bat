
@echo off
pushd "%~dp0"

set PIPENV_VENV_IN_PROJECT=1

python -m pipenv install --dev
if %errorlevel% neq 0 goto error
python -m pipenv run archive1
if %errorlevel% neq 0 goto error
copy .\c\mm-interop.dll .\dist\recognize\
if %errorlevel% neq 0 goto error

python -m pipenv requirements > requirements.txt
echo stable-ts==2.16.0 >> requirements.txt
echo punctuators==0.0.5 >> requirements.txt
echo pyinstaller==6.9.0 >> requirements.txt
echo pyinstaller-hooks-contrib==2024.7 >> requirements.txt

popd
exit

error:
popd
pause
