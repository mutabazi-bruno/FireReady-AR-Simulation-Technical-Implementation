using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FireReady.Config;
using FireReady.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FireReady.Content
{
    /// <summary>
    /// Loads and releases content through Addressables. Every handle it opens is kept and released
    /// exactly once, so loads and releases stay balanced.
    ///   Core group: loaded at start, kept for the whole run.
    ///   Scenario and Effects groups: loaded after placement, released on return to the main menu.
    /// </summary>
    public class AssetManager : MonoBehaviour
    {
        [SerializeField] string coreAudioKey = "Core/AudioCues";
        [SerializeField] string coreBriefingKey = "Core/Briefing";

        readonly List<AsyncOperationHandle> scenarioHandles = new List<AsyncOperationHandle>();
        AsyncOperationHandle<AudioCueTable> coreAudioHandle;
        AsyncOperationHandle<BriefingContent> coreBriefingHandle;
        LoadedScenario scenario;
        Task<LoadedScenario> loadingTask;

        public LoadedScenario Scenario => scenario;
        public bool ScenarioLoaded => scenario != null;
        public BriefingContent Briefing => coreBriefingHandle.IsValid() ? coreBriefingHandle.Result : null;

        public int OpenHandles => scenarioHandles.Count
            + (coreAudioHandle.IsValid() ? 1 : 0)
            + (coreBriefingHandle.IsValid() ? 1 : 0);

        // Core content is needed on every screen, so it is loaded once and kept until the app closes.
        public async Task<AudioCueTable> LoadCoreAsync()
        {
            if (coreAudioHandle.IsValid()) return coreAudioHandle.Result;

            coreAudioHandle = Addressables.LoadAssetAsync<AudioCueTable>(coreAudioKey);
            coreBriefingHandle = Addressables.LoadAssetAsync<BriefingContent>(coreBriefingKey);
            await Task.WhenAll(coreAudioHandle.Task, coreBriefingHandle.Task);

            Report("Loaded Core group");
            EventBus.Publish(new CoreContentLoaded { Briefing = Briefing });
            return coreAudioHandle.Result;
        }

        public Task<LoadedScenario> LoadScenarioAsync(string configKey)
        {
            if (scenario != null) return Task.FromResult(scenario);
            if (loadingTask != null) return loadingTask;

            loadingTask = LoadScenarioInternal(configKey);
            return loadingTask;
        }

        async Task<LoadedScenario> LoadScenarioInternal(string configKey)
        {
            EventBus.Publish(new ScenarioLoading { IsLoading = true });
            var started = Time.realtimeSinceStartup;

            try
            {
                var configHandle = Track(Addressables.LoadAssetAsync<ScenarioConfig>(configKey));
                var config = await configHandle.Task;
                if (configHandle.Status != AsyncOperationStatus.Succeeded || config == null)
                    throw new InvalidOperationException($"Scenario config '{configKey}' could not be loaded.");

                var loaded = new LoadedScenario { Config = config };

                // Start every load at once and wait for all of them (NFR-03: under 3 s).
                var layoutHandle = Track(Addressables.LoadAssetAsync<GameObject>(config.layoutPrefab));
                var extinguisherHandle = Track(Addressables.LoadAssetAsync<GameObject>(config.extinguisherPrefab));
                var effectsHandle = Track(Addressables.LoadAssetsAsync<GameObject>(config.effectsLabel, null));

                var fireHandles = new List<(FireClass fireClass, AsyncOperationHandle<GameObject> handle)>();
                foreach (var entry in config.firePrefabs)
                    fireHandles.Add((entry.fireClass, Track(Addressables.LoadAssetAsync<GameObject>(entry.prefab))));

                var waits = new List<Task> { layoutHandle.Task, extinguisherHandle.Task, effectsHandle.Task };
                foreach (var fire in fireHandles) waits.Add(fire.handle.Task);
                await Task.WhenAll(waits);

                loaded.LayoutPrefab = layoutHandle.Result;
                loaded.ExtinguisherPrefab = extinguisherHandle.Result;
                if (effectsHandle.Result != null) loaded.EffectPrefabs.AddRange(effectsHandle.Result);
                foreach (var fire in fireHandles) loaded.FirePrefabs[fire.fireClass] = fire.handle.Result;

                if (loaded.LayoutPrefab == null || loaded.ExtinguisherPrefab == null)
                    throw new InvalidOperationException("Scenario prefabs are missing from Addressables.");

                scenario = loaded;
                Report($"Loaded {config.displayName} in {Time.realtimeSinceStartup - started:0.00} s");
                return scenario;
            }
            catch
            {
                ReleaseScenario();
                throw;
            }
            finally
            {
                loadingTask = null;
                EventBus.Publish(new ScenarioLoading { IsLoading = false });
            }
        }

        AsyncOperationHandle<T> Track<T>(AsyncOperationHandle<T> handle)
        {
            scenarioHandles.Add(handle);
            return handle;
        }

        // Instances made from these prefabs must be destroyed and the pools cleared before this is called.
        public void ReleaseScenario()
        {
            int released = 0;
            foreach (var handle in scenarioHandles)
            {
                if (!handle.IsValid()) continue;
                Addressables.Release(handle);
                released++;
            }
            scenarioHandles.Clear();
            scenario = null;

            if (released > 0) Report($"Released scenario ({released} handles)");
        }

        void Report(string action)
        {
            Debug.Log($"[Assets] {action}. Open handles: {OpenHandles}");
            EventBus.Publish(new AssetHandlesChanged { OpenHandles = OpenHandles, LastAction = action });
        }

        void OnDestroy()
        {
            ReleaseScenario();
            if (coreAudioHandle.IsValid()) Addressables.Release(coreAudioHandle);
            if (coreBriefingHandle.IsValid()) Addressables.Release(coreBriefingHandle);
        }
    }
}
