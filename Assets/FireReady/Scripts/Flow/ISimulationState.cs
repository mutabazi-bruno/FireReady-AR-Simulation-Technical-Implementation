using FireReady.Core;

namespace FireReady.Flow
{
    public interface ISimulationState
    {
        SimulationStateId Id { get; }
        void Enter();
        void Tick(float dt);
        void Exit();
    }
}
