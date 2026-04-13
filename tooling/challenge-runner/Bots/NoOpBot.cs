using System.Collections.Generic;

namespace ChallengeRunner
{
    class NoOpBot : IBaselineBot
    {
        public string Name => "NoOpBot";

        public void Initialize(string playerId)
        {
            Console.WriteLine($"NoOpBot initialized for player {playerId}");
        }

        public List<ChallengeAction> DecideActions(ChallengeObservation observation)
        {
            // Do nothing - just observe
            return new List<ChallengeAction>();
        }
    }
}
