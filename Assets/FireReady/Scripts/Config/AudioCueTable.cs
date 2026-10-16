using System;
using System.Collections.Generic;
using UnityEngine;

namespace FireReady.Config
{
    public enum AudioCueId
    {
        UiClick,
        Select,
        PinPulled,
        SprayLoop,
        EffectiveHit,
        WrongAgent,
        DistanceWarning,
        FireExtinguished,
        TaskCompleted,
        SessionStart,
        SessionSuccess,
        SessionFail,
        AlarmBell,
        BreakerSwitch,
        FireCrackleLoop,
        FireRoarLoop,
        Reignite,
        EmptyClick,
        Steam,
        Placement,
        Error
    }

    public enum AudioCategory
    {
        Interface,
        Effects,
        Ambient
    }

    [Serializable]
    public class AudioCue
    {
        public AudioCueId id;
        public AudioClip clip;
        public AudioCategory category = AudioCategory.Effects;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("0 plays as 2D, 1 plays fully 3D at the source position.")]
        [Range(0f, 1f)] public float spatialBlend;
    }

    // Maps cue ids to clips. The Core group has the interface table, the scenario group has the rest.
    [CreateAssetMenu(fileName = "AudioCueTable", menuName = "FireReady/Audio Cue Table")]
    public class AudioCueTable : ScriptableObject
    {
        public List<AudioCue> cues = new List<AudioCue>();
    }
}
