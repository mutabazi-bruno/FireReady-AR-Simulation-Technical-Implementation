using UnityEngine;

namespace FireReady.Data
{
    // Small user settings live in PlayerPrefs; the session history does not.
    public class SettingsStore
    {
        const string MasterVolumeKey = "settings.masterVolume";
        const string VibrationKey = "settings.vibration";
        const string BriefingSeenKey = "settings.briefingSeen";
        const string DiagnosticsKey = "settings.diagnostics";

        public float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
            set => PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
        }

        public bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set => PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
        }

        public bool BriefingSeen
        {
            get => PlayerPrefs.GetInt(BriefingSeenKey, 0) == 1;
            set => PlayerPrefs.SetInt(BriefingSeenKey, value ? 1 : 0);
        }

        public bool ShowDiagnostics
        {
            get => PlayerPrefs.GetInt(DiagnosticsKey, 0) == 1;
            set => PlayerPrefs.SetInt(DiagnosticsKey, value ? 1 : 0);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
