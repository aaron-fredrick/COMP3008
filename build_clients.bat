@echo off
setlocal

set "CONFIGURATION=Debug"
if not "%1"=="" set "CONFIGURATION=%1"

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do set "MSBUILD=%%i"

if "%MSBUILD%"=="" (
    echo MSBuild not found. Please install Visual Studio with MSBuild.
    exit /b 1
)

echo.
echo [1/3] Building Chat.Client.Shared (%CONFIGURATION%)...
"%MSBUILD%" "Chat.Client.Shared\Chat.Client.Shared.csproj" /p:Configuration=%CONFIGURATION% /verbosity:minimal
if %ERRORLEVEL% NEQ 0 ( echo Build failed at Chat.Client.Shared! & exit /b 1 )

echo.
echo [2/3] Building Chat.Client.Polling (%CONFIGURATION%)...
"%MSBUILD%" "Chat.Client.Polling\Chat.Client.Polling.csproj" /p:Configuration=%CONFIGURATION% /verbosity:minimal
if %ERRORLEVEL% NEQ 0 ( echo Build failed at Chat.Client.Polling! & exit /b 1 )

echo.
echo [3/3] Building Chat.Client.Duplex (%CONFIGURATION%)...
"%MSBUILD%" "Chat.Client.Duplex\Chat.Client.Duplex.csproj" /p:Configuration=%CONFIGURATION% /verbosity:minimal
if %ERRORLEVEL% NEQ 0 ( echo Build failed at Chat.Client.Duplex! & exit /b 1 )

echo.
echo All client builds succeeded!
