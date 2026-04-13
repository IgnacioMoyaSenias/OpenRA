@echo off
REM OpenRA Challenge Dedicated Server Launcher
REM This script launches OpenRA with Challenge mod and dedicated server settings

echo Starting OpenRA Challenge Dedicated Server...
echo.

REM Set Challenge-specific server settings
set SERVER_ARGS=Game.Mod=challenge-ra ^
Server.RecordReplays=True ^
Server.EnableSingleplayer=True ^
Server.AllowBotOnlyGames=True ^
Challenge.Enabled=True

REM Check if MatchId is provided
if "%~1"=="" (
    echo No MatchId provided, using auto-generated ID
    set CHALLENGE_ARGS=%SERVER_ARGS%
) else (
    echo Using MatchId: %~1
    set CHALLENGE_ARGS=%SERVER_ARGS% Challenge.MatchId=%~1
)

REM Launch OpenRA with all arguments
start "" "bin\OpenRA.Server.exe" Engine.EngineDir=".." %CHALLENGE_ARGS% Server.Name="Challenge Dedicated Server" Server.ListenPort=1234

echo.
echo Challenge server started!
echo Press any key to exit...
pause > nul
