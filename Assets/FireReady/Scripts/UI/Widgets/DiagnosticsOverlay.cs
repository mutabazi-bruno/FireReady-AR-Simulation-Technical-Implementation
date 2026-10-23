using System.Text;
using FireReady.Core;
using FireReady.Flow;
using TMPro;
using UnityEngine;

namespace FireReady.UI
{
    /// <summary>
    /// Optional on-screen readout of frame rate, open Addressables handles and pool usage,
    /// so loading, releasing and pooling can be seen while the app runs. Turned on in Settings.
    /// </summary>
    public class DiagnosticsOverlay : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] float refreshInterval = 0.25f;

        readonly StringBuilder text = new StringBuilder();
        string lastAssetAction = "-";
        float timer;
        int frames;
        float elapsed;
        float worstFrame;

        void OnEnable()
        {
            EventBus.Subscribe<AssetHandlesChanged>(OnAssetHandlesChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<AssetHandlesChanged>(OnAssetHandlesChanged);
        }

        void OnAssetHandlesChanged(AssetHandlesChanged evt)
        {
            lastAssetAction = evt.LastAction;
        }

        void Update()
        {
            frames++;
            elapsed += Time.unscaledDeltaTime;
            worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);

            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = refreshInterval;

            var sim = SimulationManager.Instance;
            text.Clear();
            text.Append("FPS ").Append((frames / Mathf.Max(elapsed, 0.0001f)).ToString("0"))
                .Append("  worst ").Append((worstFrame * 1000f).ToString("0")).Append(" ms");
            if (sim != null)
            {
                text.Append("  |  ").Append(sim.CurrentState);
                text.Append("  |  handles ").Append(sim.Assets.OpenHandles).Append('\n');
                text.Append("Last: ").Append(lastAssetAction);
                text.Append("  |  pools ").Append(sim.Pools.DescribeStats());
            }

            if (label != null) label.SetText(text);
            frames = 0;
            elapsed = 0f;
            worstFrame = 0f;
        }
    }
}
