using FireReady.Core;

namespace FireReady.Flow
{
    public class MainMenuState : SimulationStateBase
    {
        public MainMenuState(SimulationManager manager) : base(manager) { }

        public override SimulationStateId Id => SimulationStateId.MainMenu;

        public override void Enter()
        {
            // Leaving a session for the menu frees the scenario content (DMP 6.4).
            manager.TeardownScenario();
        }
    }
}
