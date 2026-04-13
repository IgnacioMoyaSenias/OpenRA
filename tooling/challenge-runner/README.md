# Challenge Runner

Baseline gateway client for testing the OpenRA Challenge bridge.

## Overview

This tool provides baseline bots that can connect to the Challenge gateway and complete full games. It serves as a reference implementation and testing tool for the Challenge protocol.

## Available Bots

### NoOpBot
- **Behavior**: Does nothing, only observes the game
- **Purpose**: Test basic connectivity and observation handling
- **Usage**: `dotnet run noop`

### RandomMoveBot
- **Behavior**: Moves random units to random positions
- **Purpose**: Test movement actions and basic decision making
- **Usage**: `dotnet run randommove`

### GreedyAttackVisibleBot
- **Behavior**: Attacks visible enemies, moves randomly when no enemies visible
- **Purpose**: Test combat actions and tactical decision making
- **Usage**: `dotnet run greedyattack`

## Usage

```bash
# Run a specific bot with optional port
dotnet run <bot-type> [match-id] [port]

# Examples
dotnet run noop test-match-001 50051
dotnet run randommove demo-match 50052
dotnet run greedyattack test-match-003 50051

# If no match-id provided, uses a random test ID
dotnet run noop
```

## Protocol Support

The runner implements the Challenge protocol:

- **Connection**: TCP to `tcp://127.0.0.1:{port}` (configurable port)
- **Format**: JSONL (JSON Lines) messages for observations and actions
- **Actions**: Move, attack, build, harvest, set_rally, sell
- **Observations**: Unit positions, resources, game state
- **Dual Port Mode**: Supports multiple bots on different ports (50051, 50052)

## Testing

1. Start OpenRA with Challenge mode:
   ```bash
   ./launch-challenge-dedicated.cmd test-match-001
   ```

2. Run a baseline bot:
   ```bash
   cd tooling/challenge-runner
   dotnet run greedyattack test-match-001
   ```

3. Verify:
   - Bot connects successfully
   - Observations are received
   - Actions are sent
   - Game completes with replay and results

## Architecture

```
ChallengeRunner/
├── Program.cs              # Main entry point and game loop
├── IBaselineBot.cs         # Bot interface
├── ChallengeGatewayClient.cs # WebSocket client
├── ChallengeProtocol.cs     # Protocol classes
├── Bots/
│   ├── NoOpBot.cs         # No-operation bot
│   ├── RandomMoveBot.cs    # Random movement bot
│   └── GreedyAttackVisibleBot.cs # Greedy attack bot
└── README.md              # This file
```

## Requirements

- .NET 8.0
- OpenRA with Challenge mod
- WebSocket gateway server (part of Challenge mod)

## Output

When running, the bot will:
- Log connection status
- Log received observations
- Log sent actions
- Complete games and leave replay/results files

The baseline bots are designed to be simple but functional, demonstrating the complete Challenge protocol flow.
