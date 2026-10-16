using FireReady.Core;

namespace FireReady.Flow
{
    /// <summary>
    /// The simulation itself. Ticks every simulation system in a fixed order:
    /// input and spray first, then fires, so suppression applied this frame stops growth this frame.
    /// </summary>
    public class RunningState : SimulationStateBase
    {
        public RunningState(SimulationManager manager) : base(manager) { }

        public override SimulationStateId Id => SimulationStateId.Running;

        public override void Enter()
        {
            if (!manager.SessionActive) manager.BeginSession();
            manager.Interaction.SetInputEnabled(true);
        }

        public override void Tick(float dt)
        {
            manager.AdvanceClock(dt);
            manager.Interaction.Tick(dt);
            manager.Entities.Tick(dt, manager.SessionTime);

            if (manager.SessionActive && manager.Entities.AllFiresSafelyOut)
                manager.EndSession(SessionOutcome.Completed, "Every fire is out and stayed out");
        }

        public override void Exit()
        {
            manager.Interaction.SetInputEnabled(false);
        }
    }
}
