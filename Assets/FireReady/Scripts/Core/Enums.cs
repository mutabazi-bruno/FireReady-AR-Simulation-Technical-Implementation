namespace FireReady.Core
{
    public enum FireClass
    {
        ClassA,
        ClassB,
        Electrical
    }

    public enum FireState
    {
        Dormant,
        Small,
        Growing,
        OutOfControl,
        Extinguished
    }

    public enum AgentType
    {
        Water,
        Foam,
        CO2,
        DryPowder
    }

    public enum AgentEffect
    {
        Effective,
        Weak,
        Dangerous,
        Fatal
    }

    public enum SessionOutcome
    {
        Completed,
        Evacuated,
        Failed,
        Abandoned
    }

    public enum SimulationStateId
    {
        None,
        MainMenu,
        Briefing,
        ARSetup,
        Running,
        Paused,
        Summary
    }

    public enum DistanceBand
    {
        NoTarget,
        InRange,
        TooClose,
        TooFar
    }

    public enum DischargeBlockReason
    {
        PinIn,
        Empty
    }

    public enum PlacementStage
    {
        Idle,
        Scanning,
        ReadyToPlace,
        Previewing,
        Placed
    }

    public static class EnumText
    {
        public static string FireClassName(FireClass fireClass)
        {
            switch (fireClass)
            {
                case FireClass.ClassA: return "Class A";
                case FireClass.ClassB: return "Class B";
                default: return "Electrical";
            }
        }

        public static string AgentName(AgentType agent)
        {
            switch (agent)
            {
                case AgentType.Water: return "Water";
                case AgentType.Foam: return "Foam";
                case AgentType.CO2: return "CO2";
                default: return "Dry powder";
            }
        }

        public static string FireStateName(FireState state)
        {
            switch (state)
            {
                case FireState.Small: return "Small";
                case FireState.Growing: return "Growing";
                case FireState.OutOfControl: return "Out of control";
                case FireState.Extinguished: return "Out";
                default: return "Not burning";
            }
        }
    }
}
