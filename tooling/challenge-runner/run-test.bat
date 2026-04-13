@echo off
REM Test runner for Challenge baseline bots

echo Starting Challenge baseline bot test...
echo.

REM Build the project
echo Building challenge-runner...
dotnet build -c Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Build successful!
echo.

REM Run tests using the compiled DLL directly (no rebuild)
echo Testing NoOpBot on port 50051...
start "NoOpBot" cmd /k "cd /d %~dp0 && dotnet ..\bin\challenge-runner.dll noop test-match-001 50051"

timeout /t 2 > nul

echo Testing RandomMoveBot on port 50051...
start "RandomMoveBot" cmd /k "cd /d %~dp0 && dotnet ..\bin\challenge-runner.dll randommove test-match-001 50051"

timeout /t 2 > nul

echo Testing GreedyAttackBot on port 50051...
start "GreedyAttackBot" cmd /k "cd /d %~dp0 && dotnet ..\bin\challenge-runner.dll greedyattack test-match-001 50051"

echo.
echo All test bots started in separate windows.
echo Make sure OpenRA Challenge server is running:
echo   launch-challenge-dedicated.cmd test-match-001
echo.
pause
