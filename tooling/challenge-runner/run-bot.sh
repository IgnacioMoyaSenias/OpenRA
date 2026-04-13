#!/bin/bash

# Run a single Challenge baseline bot

if [ $# -eq 0 ]; then
    echo "Usage: ./run-bot.sh <bot-type> [match-id] [port]"
    echo "Available bots: noop, randommove, greedyattack"
    echo "Example: ./run-bot.sh greedyattack test-match-001 50051"
    exit 1
fi

BOT_TYPE=$1
MATCH_ID=${2:-"test-match-001"}
PORT=${3:-"50051"}

if [ -z "$2" ]; then
    echo "No match ID provided, using: $MATCH_ID"
else
    echo "Using match ID: $MATCH_ID"
fi

if [ -z "$3" ]; then
    echo "No port provided, using: $PORT"
else
    echo "Using port: $PORT"
fi

echo "Starting $BOT_TYPE bot..."
cd "$(dirname "$0")"
dotnet run --project challenge-runner.csproj "$BOT_TYPE" "$MATCH_ID" "$PORT"

echo "Bot execution completed."
