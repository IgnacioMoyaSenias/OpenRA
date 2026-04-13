using System;
using System.Collections.Generic;
using System.Linq;

namespace ChallengeRunner
{
    class GreedyAttackVisibleBot : IBaselineBot
    {
        private string playerId = string.Empty;
        private readonly Random random = new Random();

        public string Name => "GreedyAttackVisibleBot";

        public void Initialize(string playerId)
        {
            this.playerId = playerId;
            Console.WriteLine($"GreedyAttackVisibleBot initialized for player {playerId}");
        }

        public List<ChallengeAction> DecideActions(ChallengeObservation observation)
        {
            var actions = new List<ChallengeAction>();

            // Attack visible enemies if we have units
            if (observation.VisibleEnemies?.Count > 0 && observation.OwnUnits?.Count > 0)
            {
                // Find our units that can attack
                var attackUnits = observation.OwnUnits.Where(u =>
                    u.Type.Contains("infantry") || u.Type.Contains("vehicle") || u.Type.Contains("tank")
                ).ToList();

                if (attackUnits.Count > 0)
                {
                    // Attack each visible enemy with a random unit
                    foreach (var enemy in observation.VisibleEnemies.Take(3)) // Limit to 3 attacks per turn
                    {
                        var attacker = attackUnits[random.Next(attackUnits.Count)];

                        actions.Add(new ChallengeAction
                        {
                            Kind = "attack",
                            UnitId = attacker.Id,
                            TargetActorId = enemy.Id
                        });
                    }
                }
            }

            // If no enemies to attack, move units randomly
            if (actions.Count == 0 && observation.OwnUnits?.Count > 0)
            {
                var unitIndex = random.Next(observation.OwnUnits.Count);
                var unit = observation.OwnUnits[unitIndex];

                var targetX = random.Next(observation.Map.Width);
                var targetY = random.Next(observation.Map.Height);

                actions.Add(new ChallengeAction
                {
                    Kind = "move",
                    UnitId = unit.Id,
                    Target = new ChallengePosition { X = targetX, Y = targetY }
                });
            }

            return actions;
        }
    }
}
