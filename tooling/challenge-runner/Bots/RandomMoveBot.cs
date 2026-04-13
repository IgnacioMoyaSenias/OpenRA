using System;
using System.Collections.Generic;

namespace ChallengeRunner
{
    class RandomMoveBot : IBaselineBot
    {
        private string playerId = string.Empty;
        private readonly Random random = new Random();

        public string Name => "RandomMoveBot";

        public void Initialize(string playerId)
        {
            this.playerId = playerId;
            Console.WriteLine($"RandomMoveBot initialized for player {playerId}");
        }

        public List<ChallengeAction> DecideActions(ChallengeObservation observation)
        {
            var actions = new List<ChallengeAction>();

            // Move random units if we have any
            if (observation.OwnUnits?.Count > 0)
            {
                // Pick a random unit to move
                var unitIndex = random.Next(observation.OwnUnits.Count);
                var unit = observation.OwnUnits[unitIndex];

                // Pick a random target position within map bounds
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
