using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    // NFR-06: volume and vibration can be changed. Diagnostics shows the performance overlay.
    public class SettingsScreen : UIScreen
    {
        [SerializeField] Slider volumeSlider;
        [SerializeField] TMP_Text volumeValue;
        [SerializeField] Toggle vibrationToggle;
        [SerializeField] Toggle diagnosticsToggle;
        [SerializeField] TMP_Text storageText;
        [SerializeField] Button backButton;

        bool refreshing;

        protected override void BindButtons()
        {
            Bind(backButton, () =>
            {
                Sim.Data.Settings.Save();
                UI.ShowMainMenu();
            });

            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
            if (diagnosticsToggle != null) diagnosticsToggle.onValueChanged.AddListener(OnDiagnosticsChanged);
        }

        protected override void OnShow()
        {
            refreshing = true;
            var settings = Sim.Data.Settings;
            if (volumeSlider != null) volumeSlider.value = settings.MasterVolume;
            if (vibrationToggle != null) vibrationToggle.isOn = settings.VibrationEnabled;
            if (diagnosticsToggle != null) diagnosticsToggle.isOn = settings.ShowDiagnostics;
            UpdateVolumeLabel(settings.MasterVolume);
            if (storageText != null) storageText.text = $"History file: {Sim.Data.StoragePath}";
            refreshing = false;
        }

        void OnVolumeChanged(float value)
        {
            UpdateVolumeLabel(value);
            if (refreshing) return;
            Sim.Data.Settings.MasterVolume = value;
            if (Sim.Audio != null) Sim.Audio.ApplySettings();
        }

        void OnVibrationChanged(bool on)
        {
            if (!refreshing) Sim.Data.Settings.VibrationEnabled = on;
        }

        void OnDiagnosticsChanged(bool on)
        {
            if (refreshing) return;
            Sim.Data.Settings.ShowDiagnostics = on;
            UI.SetDiagnosticsVisible(on);
        }

        void UpdateVolumeLabel(float value)
        {
            if (volumeValue != null) volumeValue.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
