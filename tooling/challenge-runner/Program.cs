using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ChallengeRunner
{
    class Program
    {
        private static readonly Dictionary<string, IBaselineBot> bots = new()
        {
            ["noop"] = new NoOpBot(),
            ["randommove"] = new RandomMoveBot(),
            ["greedyattack"] = new GreedyAttackVisibleBot()
        };

        static async Task Main(string[] args)
        {
            Console.WriteLine("Challenge Runner - Baseline Gateway");
            Console.WriteLine("Available bots: noop, randommove, greedyattack");

            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run <bot-type> [match-id] [port]");
                Console.WriteLine("Example: dotnet run noop test-match-001 50051");
                return;
            }

            var botType = args[0].ToLower();
            var matchId = args.Length > 1 ? args[1] : "demo-match";
            var port = args.Length > 2 ? int.Parse(args[2]) : 50051;

            if (!bots.ContainsKey(botType))
            {
                Console.WriteLine($"Unknown bot type: {botType}");
                return;
            }

            Console.WriteLine($"Starting {botType} bot with match ID: {matchId}");

            var bot = bots[botType];
            var gateway = new ChallengeGatewayClientTCP(matchId, botType, "127.0.0.1", port);

            try
            {
                await RunBotAsync(gateway, bot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("Bot execution completed. Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task RunBotAsync(ChallengeGatewayClientTCP gateway, IBaselineBot bot)
        {
            var gameState = new GameState { PlayerId = bot.Name };
            var currentDecisionId = 0;

            // Handle incoming messages
            gateway.OnMessage += (message) =>
            {
                Console.WriteLine($"Received: {message}");

                try
                {
                    var jsonDoc = JsonDocument.Parse(message);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("type", out var typeProp))
                    {
                        var messageType = typeProp.GetString();

                        switch (messageType)
                        {
                            case "init":
                                HandleInit(root, gameState, bot);
                                break;

                            case "observation":
                                HandleObservation(root, gameState, bot);
                                break;

                            case "end":
                                Console.WriteLine("Game ended");
                                return;

                            default:
                                Console.WriteLine($"Unknown message type: {messageType}");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing message: {ex.Message}");
                }
            };

            // Connect and start the game loop
            await gateway.ConnectAsync();

            // Game loop - respond to observations with actions
            while (true)
            {
                await Task.Delay(100); // Small delay to prevent busy waiting

                if (gameState.LastObservation != null && !gameState.ActionSent)
                {
                    var actions = bot.DecideActions(gameState.LastObservation);
                    await SendActionsAsync(gateway, currentDecisionId++, actions);
                    gameState.ActionSent = true;
                }
            }
        }

        private static void HandleInit(JsonElement root, GameState gameState, IBaselineBot bot)
        {
            if (root.TryGetProperty("content", out var contentProp) &&
                contentProp.TryGetProperty("player_id", out var playerIdProp))
            {
                gameState.PlayerId = playerIdProp.GetString();
                Console.WriteLine($"Initialized with player ID: {gameState.PlayerId}");
                bot.Initialize(gameState.PlayerId);
            }
        }

        private static void HandleObservation(JsonElement root, GameState gameState, IBaselineBot bot)
        {
            try
            {
                var observation = new ChallengeObservation(root.GetProperty("content"));
                gameState.LastObservation = observation;
                gameState.ActionSent = false; // Reset for next decision

                Console.WriteLine($"Received observation at tick {observation.Tick}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling observation: {ex.Message}");
            }
        }

        private static async Task SendActionsAsync(ChallengeGatewayClientTCP gateway, int decisionId, List<ChallengeAction> actions)
        {
            if (actions.Count == 0)
                return;

            var actionBatch = new ChallengeActionBatch
            {
                DecisionId = decisionId,
                Actions = actions
            };

            var json = JsonSerializer.Serialize(actionBatch);
            await gateway.SendMessageAsync(json);
            Console.WriteLine($"Sent {actions.Count} actions for decision {decisionId}");
        }
    }

    internal class GameState
    {
        public string PlayerId { get; set; } = string.Empty;
        public ChallengeObservation? LastObservation { get; set; }
        public bool ActionSent { get; set; }
    }
}
