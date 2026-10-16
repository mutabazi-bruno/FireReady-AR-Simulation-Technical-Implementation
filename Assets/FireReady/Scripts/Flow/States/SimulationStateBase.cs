using FireReady.Core;

namespace FireReady.Flow
{
    // Shared plumbing for the states: a reference to the manager they drive and empty defaults.
    public abstract class SimulationStateBase : ISimulationState
    {
        protected readonly SimulationManager manager;

        protected SimulationStateBase(SimulationManager manager)
        {
            this.manager = manager;
        }

        public abstract SimulationStateId Id { get; }
        public virtual void Enter() { }
        public virtual void Tick(float dt) { }
        public virtual void Exit() { }
    }
}
