@echo off
setlocal

set "PROJECT_DIR=%~dp0"
if "%PROJECT_DIR:~-1%"=="\" set "PROJECT_DIR=%PROJECT_DIR:~0,-1%"
cd /d "%PROJECT_DIR%"

set "GAME_EXE="
for %%D in ("%PROJECT_DIR%\Builds" "%PROJECT_DIR%\Build" "%PROJECT_DIR%\Release") do (
    if exist "%%~fD\" (
        for /r "%%~fD" %%F in (*.exe) do (
            if not defined GAME_EXE if /i not "%%~nxF"=="UnityCrashHandler64.exe" set "GAME_EXE=%%~fF"
        )
    )
)

if defined GAME_EXE (
    echo Launching built game:
    echo %GAME_EXE%
    start "" "%GAME_EXE%"
    exit /b 0
)

set "UNITY_VERSION="
for /f "tokens=2 delims=:" %%A in ('findstr /b /c:"m_EditorVersion:" "%PROJECT_DIR%\ProjectSettings\ProjectVersion.txt"') do set "UNITY_VERSION=%%A"
set "UNITY_VERSION=%UNITY_VERSION: =%"

set "UNITY_EXE="
if "%UNITY_VERSION%"=="" goto FIND_UNITY_ON_PATH

set "UNITY_CANDIDATE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if exist "%UNITY_CANDIDATE%" set "UNITY_EXE=%UNITY_CANDIDATE%"
if defined UNITY_EXE goto LAUNCH_EDITOR

set "UNITY_CANDIDATE=%ProgramFiles(x86)%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if exist "%UNITY_CANDIDATE%" set "UNITY_EXE=%UNITY_CANDIDATE%"
if defined UNITY_EXE goto LAUNCH_EDITOR

set "UNITY_CANDIDATE=%LOCALAPPDATA%\Programs\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if exist "%UNITY_CANDIDATE%" set "UNITY_EXE=%UNITY_CANDIDATE%"
if defined UNITY_EXE goto LAUNCH_EDITOR

:FIND_UNITY_ON_PATH
for /f "delims=" %%U in ('where Unity.exe 2^>nul') do if not defined UNITY_EXE set "UNITY_EXE=%%U"
if defined UNITY_EXE goto LAUNCH_EDITOR

echo No built game was found, and Unity Editor was not found.
echo Expected Unity version: %UNITY_VERSION%
echo.
echo Install the matching Unity Editor, or build the game into Builds, Build, or Release.
if not defined LAUNCHER_NO_PAUSE pause
exit /b 1

:LAUNCH_EDITOR
echo No built game found. Opening Unity and starting from MainMenu...
echo %UNITY_EXE%
start "" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%" -executeMethod WarOfEras.EditorTools.AutoPlayFromMainMenu.Run
exit /b 0
