using FireReady.Core;

namespace FireReady.Flow
{
    // Holds the current stage of the session and runs its Enter, Tick and Exit.
    public class SimulationStateMachine
    {
        public ISimulationState Current { get; private set; }
        public SimulationStateId CurrentId => Current?.Id ?? SimulationStateId.None;

        public void ChangeState(ISimulationState next)
        {
            if (next == null || next == Current) return;

            var previous = CurrentId;
            Current?.Exit();
            Current = next;

            // Published before Enter so listeners have switched over by the time the state starts work.
            EventBus.Publish(new SimulationStateChanged { Previous = previous, Current = next.Id });
            Current.Enter();
        }

        public void Tick(float dt)
        {
            Current?.Tick(dt);
        }
    }
}
