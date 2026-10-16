using FireReady.Data;
using FireReady.Simulation;
using UnityEngine;

namespace FireReady.Core
{
    // Flow

    public struct SimulationStateChanged
    {
        public SimulationStateId Previous;
        public SimulationStateId Current;
    }

    public struct SessionStarted
    {
        public string ScenarioId;
    }

    public struct SessionEnded
    {
        public SessionOutcome Outcome;
        public string Reason;
    }

    public struct SessionSaved
    {
        public SessionRecord Record;
    }

    public struct HistoryCleared { }

    // AR

    public struct ARAvailabilityChecked
    {
        public bool Supported;
        public string Message;
    }

    public struct PlacementStageChanged
    {
        public PlacementStage Stage;
    }

    public struct PlaneScanUpdated
    {
        public float LargestWidth;
        public float LargestLength;
        public float Progress;
        public bool Ready;
    }

    public struct ScenePlaced
    {
        public Transform Root;
    }

    public struct TrackingLost
    {
        public string Reason;
    }

    public struct TrackingRestored { }

    // Assets

    public struct ScenarioLoading
    {
        public bool IsLoading;
    }

    public struct CoreContentLoaded
    {
        public FireReady.Config.BriefingContent Briefing;
    }

    public struct AssetHandlesChanged
    {
        public int OpenHandles;
        public string LastAction;
    }

    // Fires

    public struct FireIgnited
    {
        public FireSource Fire;
    }

    public struct FireStateChanged
    {
        public FireSource Fire;
        public FireState Previous;
        public FireState Current;
    }

    public struct FireExtinguished
    {
        public FireSource Fire;
    }

    public struct FireReignited
    {
        public FireSource Fire;
        public string Cause;
    }

    public struct FireBeyondControl
    {
        public FireSource Fire;
    }

    public struct AgentApplied
    {
        public FireSource Fire;
        public AgentType Agent;
        public AgentEffect Effect;
        public bool OnBase;
        public float Distance;
        public float DeltaTime;
        public int BurstId;
    }

    // Extinguishers

    public struct ExtinguisherSelected
    {
        public Extinguisher Extinguisher;
    }

    public struct ExtinguisherDropped
    {
        public Extinguisher Extinguisher;
    }

    public struct PinPulled
    {
        public Extinguisher Extinguisher;
    }

    public struct DischargeBlocked
    {
        public Extinguisher Extinguisher;
        public DischargeBlockReason Reason;
    }

    public struct SprayStarted
    {
        public Extinguisher Extinguisher;
    }

    public struct SprayStopped
    {
        public Extinguisher Extinguisher;
        public float Duration;
    }

    public struct ExtinguisherEmpty
    {
        public Extinguisher Extinguisher;
    }

    // Interaction

    public struct AimUpdated
    {
        public bool HasFire;
        public string FireName;
        public float Distance;
        public DistanceBand Band;
        public bool OnBase;
    }

    public struct AimTargetChanged
    {
        public SimulationEntity Entity;
        public string Label;
        public string Hint;
    }

    public struct DistanceWarning
    {
        public DistanceBand Band;
        public float Distance;
    }

    // Spray time while a fire burns but the spray is not on any fire. Counts against aim accuracy.
    public struct SprayMissed
    {
        public float DeltaTime;
    }

    public struct TooCloseRecorded
    {
        public float Distance;
    }

    public struct HintRequested
    {
        public string Message;
    }

    // Procedure

    public struct AlarmRaised { }

    public struct PowerIsolated { }

    public struct EvacuationChosen { }

    public struct TaskCompleted
    {
        public TrainingTask Task;
    }

    public struct TasksChanged { }

    // Performance

    public struct MistakeRecorded
    {
        public MistakeEntry Mistake;
        public bool Critical;
    }

    public struct PerformanceUpdated
    {
        public int Score;
        public float AimAccuracy;
        public int MistakeCount;
    }

    // UI

    public struct UiClicked { }
}
