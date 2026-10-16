using FireReady.Core;

namespace FireReady.Flow
{
    // Saving happens here, when the summary opens, never during the simulation (DMP 6.3).
    public class SummaryState : SimulationStateBase
    {
        public SummaryState(SimulationManager manager) : base(manager) { }

        public override SimulationStateId Id => SimulationStateId.Summary;

        public override void Enter()
        {
            manager.SaveLastSession();
        }
    }
}
