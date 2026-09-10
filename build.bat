@echo off
setlocal

set "PROJECT=Chat.Server"
set "CONFIGURATION=Debug"

if not "%1"=="" set "PROJECT=%1"
if not "%2"=="" set "CONFIGURATION=%2"

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do set "MSBUILD=%%i"

if "%MSBUILD%"=="" (
    echo MSBuild not found. Please install Visual Studio with MSBuild.
    exit /b 1
)

rem Accept a .sln/.slnx or .csproj path directly, or a bare project name.
rem Bare project names are looked up under src\ first, then tests\.
if "%PROJECT:~-4%"==".sln" (
    set "PROJECT_PATH=%PROJECT%"
    goto :build
)
if "%PROJECT:~-5%"==".slnx" (
    set "PROJECT_PATH=%PROJECT%"
    goto :build
)
if "%PROJECT:~-7%"==".csproj" (
    set "PROJECT_PATH=%PROJECT%"
    goto :build
)

set "PROJECT_PATH=src\%PROJECT%\%PROJECT%.csproj"
if not exist "%PROJECT_PATH%" set "PROJECT_PATH=tests\%PROJECT%\%PROJECT%.csproj"

:build
if not exist "%PROJECT_PATH%" (
    echo Project file not found: %PROJECT_PATH%
    exit /b 1
)

echo Building %PROJECT_PATH% (%CONFIGURATION%)...
"%MSBUILD%" "%PROJECT_PATH%" /p:Configuration=%CONFIGURATION% /verbosity:minimal

if %ERRORLEVEL% EQU 0 (
    echo Build succeeded!
) else (
    echo Build failed!
    exit /b 1
)
