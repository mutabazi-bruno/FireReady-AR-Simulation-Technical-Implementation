using FireReady.Core;

namespace FireReady.Flow
{
    // Nothing is ticked here, so fires, timers and the session clock all stay frozen (FR-23).
    public class PausedState : SimulationStateBase
    {
        public PausedState(SimulationManager manager) : base(manager) { }

        public override SimulationStateId Id => SimulationStateId.Paused;

        public bool PausedByTracking { get; private set; }

        public void Prepare(bool byTracking)
        {
            PausedByTracking = byTracking;
        }
    }
}
