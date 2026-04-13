#!/bin/bash

# OpenRA Challenge Dedicated Server Launcher
# This script launches OpenRA with Challenge mod and dedicated server settings

echo "Starting OpenRA Challenge Dedicated Server..."
echo

# Set Challenge-specific server settings
SERVER_ARGS="Game.Mod=challenge-ra Server.RecordReplays=True Server.EnableSingleplayer=True Server.AllowBotOnlyGames=True Challenge.Enabled=True"

# Check if MatchId is provided
if [ -z "$1" ]; then
    echo "No MatchId provided, using auto-generated ID"
    CHALLENGE_ARGS="$SERVER_ARGS"
else
    echo "Using MatchId: $1"
    CHALLENGE_ARGS="$SERVER_ARGS Challenge.MatchId=$1"
fi

# Launch OpenRA with all arguments
./OpenRA.AppImage $CHALLENGE_ARGS Server.Name="Challenge Dedicated Server" Server.ListenPort=1234

echo
echo "Challenge server started!"
echo "Press Ctrl+C to exit..."
wait
