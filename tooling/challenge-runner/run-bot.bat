@echo off
REM Run a single Challenge baseline bot

if "%~1"=="" (
    echo Usage: run-bot.bat ^<bot-type^> [match-id] [port]
    echo Available bots: noop, randommove, greedyattack
    echo Example: run-bot.bat greedyattack test-match-001 50051
    pause
    exit /b 1
)

set BOT_TYPE=%~1
set MATCH_ID=%~2
set PORT=%~3

if "%~2"=="" (
    set MATCH_ID=test-match-001
    echo No match ID provided, using: %MATCH_ID%
) else (
    echo Using match ID: %MATCH_ID%
)

if "%~3"=="" (
    set PORT=50051
    echo No port provided, using: %PORT%
) else (
    echo Using port: %PORT%
)

echo Starting %BOT_TYPE% bot...
cd /d %~dp0
dotnet run --project challenge-runner.csproj %BOT_TYPE% %MATCH_ID% %PORT%

echo Bot execution completed.
pause
