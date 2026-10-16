using FireReady.Core;

namespace FireReady.Flow
{
    /// <summary>
    /// Scanning and placing. Used for the first placement and for Reposition from the pause menu,
    /// in which case the session clock stays frozen and the running session continues afterwards.
    /// </summary>
    public class ARSetupState : SimulationStateBase
    {
        public ARSetupState(SimulationManager manager) : base(manager) { }

        public override SimulationStateId Id => SimulationStateId.ARSetup;

        public bool IsReposition { get; private set; }

        public void Prepare(bool reposition)
        {
            IsReposition = reposition;
        }

        public override void Enter()
        {
            EventBus.Subscribe<ScenePlaced>(OnScenePlaced);

            if (IsReposition) manager.AR.BeginReposition();
            else manager.AR.BeginScanning();
        }

        public override void Exit()
        {
            EventBus.Unsubscribe<ScenePlaced>(OnScenePlaced);
        }

        void OnScenePlaced(ScenePlaced evt)
        {
            manager.OnScenePlaced(IsReposition);
        }
    }
}
