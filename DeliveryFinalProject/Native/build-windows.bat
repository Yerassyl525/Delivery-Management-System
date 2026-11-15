@echo off
setlocal
cd /d "%~dp0"
where cl >nul 2>nul
if errorlevel 1 (
  echo Microsoft C/C++ compiler not found. Open an x64 Native Tools Command Prompt for Visual Studio.
  exit /b 1
)
cl /nologo /O2 /LD /TC delivery_native.c /Fe:delivery_native.dll
if errorlevel 1 exit /b 1
echo Built %CD%\delivery_native.dll
endlocal

