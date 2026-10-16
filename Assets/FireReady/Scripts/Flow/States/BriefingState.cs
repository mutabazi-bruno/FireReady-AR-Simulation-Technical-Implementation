using FireReady.Core;

namespace FireReady.Flow
{
    // Shows the goal, the four extinguishers and the PASS steps. The UI handles the pages.
    public class BriefingState : SimulationStateBase
    {
        public BriefingState(SimulationManager manager) : base(manager) { }

        public override SimulationStateId Id => SimulationStateId.Briefing;

        public override void Exit()
        {
            if (manager.Data != null) manager.Data.Settings.BriefingSeen = true;
        }
    }
}
