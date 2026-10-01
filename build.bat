@echo off
setlocal EnableExtensions

rem ============================================================
rem ELEMENTAL BATTLEGROUNDS ARSENAL - BUILD
rem
rem This builds the mod but does NOT install/deploy it.
rem
rem You need:
rem   1. Your ULTRAKILL install folder
rem   2. A BepInEx/r2modman profile containing PluginConfigurator
rem
rem The finished DLL will be placed in:
rem   bin\Release\netstandard2.1\ElementalBattlegrounds.dll
rem ============================================================

set "ROOT=%~dp0"
set "PROJECT=%ROOT%ElementalBattlegrounds.csproj"
set "BUILT=%ROOT%bin\Release\netstandard2.1\ElementalBattlegrounds.dll"

echo ============================================================
echo ELEMENTAL BATTLEGROUNDS ARSENAL - BUILD
echo ============================================================
echo.

if not exist "%PROJECT%" (
    echo ERROR: Could not find:
    echo   %PROJECT%
    goto :fail
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: The .NET SDK was not found in PATH.
    echo Install the .NET SDK and try again.
    goto :fail
)

echo Enter your ULTRAKILL installation folder.
echo Example:
echo   C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL
echo.
set /p "ULTRAKILL=ULTRAKILL folder: "

rem Remove quotation marks if the path was pasted/dragged with quotes.
set "ULTRAKILL=%ULTRAKILL:"=%"

if not exist "%ULTRAKILL%\ULTRAKILL_Data\Managed\Assembly-CSharp.dll" (
    echo.
    echo ERROR: That does not appear to be the ULTRAKILL folder.
    echo Could not find:
    echo   %ULTRAKILL%\ULTRAKILL_Data\Managed\Assembly-CSharp.dll
    goto :fail
)

echo.
echo Enter the folder containing the BepInEx installation you want
echo to use for build references.
echo.
echo For r2modman, this is the PROFILE folder, for example:
echo   C:\Users\You\AppData\Roaming\r2modmanPlus-local\ULTRAKILL\profiles\Default
echo.
echo If BepInEx is installed directly inside the ULTRAKILL folder,
echo leave this blank and press Enter.
echo.
set /p "PROFILE=BepInEx/r2modman profile folder: "

set "PROFILE=%PROFILE:"=%"

echo.
echo ============================================================
echo BUILDING
echo ============================================================
echo Game:
echo   %ULTRAKILL%

if defined PROFILE (
    echo BepInEx profile:
    echo   %PROFILE%
) else (
    echo BepInEx:
    echo   Using the installation inside the ULTRAKILL folder.
)

echo.

if defined PROFILE (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%build.ps1" -GameDir "%ULTRAKILL%" -ProfileDir "%PROFILE%"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%build.ps1" -GameDir "%ULTRAKILL%"
)

if errorlevel 1 goto :fail

if not exist "%BUILT%" (
    echo.
    echo ERROR: The build reported success, but the DLL was not found:
    echo   %BUILT%
    goto :fail
)

echo.
echo ============================================================
echo BUILD SUCCEEDED
echo ============================================================
echo.
echo Finished DLL:
echo   %BUILT%
echo.
echo Copy ElementalBattlegrounds.dll into the appropriate
echo BepInEx\plugins folder to install it.
echo.
pause
exit /b 0

:fail
echo.
echo ============================================================
echo BUILD FAILED
echo ============================================================
echo.
pause
exit /b 1