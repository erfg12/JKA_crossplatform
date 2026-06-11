@echo off

net session >nul 2>&1
if %errorLevel% == 0 (
    goto :AdminStarted
) else (
    echo Requesting administrative privileges...
    powershell -Command "Start-Process -FilePath '%0' -Verb RunAs"
    exit /b
)

:AdminStarted
cd /d "%~dp0"

:: Check if the C# file actually exists before trying to run it
if exist "matchmaking.cs" (
    dotnet run matchmaking.cs
) else (
    echo Error: matchmaking.cs was not found in this folder.
)

pause