using System.Collections.Generic;

namespace ChallengeRunner
{
    interface IBaselineBot
    {
        string Name { get; }
        
        void Initialize(string playerId);
        
        List<ChallengeAction> DecideActions(ChallengeObservation observation);
    }
}
