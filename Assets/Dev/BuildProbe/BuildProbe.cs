using UnityEngine;

namespace Nekuzaky.HorrorAI.Dev
{
    public sealed class BuildProbe : MonoBehaviour
    {
        #region Private and Protected

        private const int WarmupFrames = 120;
        private const float NoiseAt = 2f;
        private const float QuitAt = 9f;

        private int _frames;
        private int _measured;
        private double _milliseconds;
        private long _allocated;
        private int _transitions;
        private bool _noised;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("Build Probe");
            DontDestroyOnLoad(host);
            host.AddComponent<BuildProbe>();
            Debug.Log("[BuildProbe] installed");
        }

        private void OnEnable() => HorrorAI.StateChanged += OnStateChanged;

        private void OnDisable() => HorrorAI.StateChanged -= OnStateChanged;

        private void OnStateChanged(HorrorEnemy enemy, HorrorTransition transition)
        {
            _transitions++;
            Debug.Log("[BuildProbe] " + enemy.name + ": " + transition.From + " > " + transition.To + " (" + transition.Explanation + ")");
        }

        private void Update()
        {
            _frames++;
            var stats = HorrorAI.Stats;
            if (_frames > WarmupFrames)
            {
                _measured++;
                _milliseconds += stats.Milliseconds;
                _allocated += stats.AllocatedBytes;
            }
            if (!_noised && Time.time >= NoiseAt && HorrorAI.Enemies.Count > 0)
            {
                _noised = true;
                var enemy = HorrorAI.Enemies[0].transform;
                HorrorAI.EmitNoise(enemy.position + enemy.forward * 3f, 15f);
                Debug.Log("[BuildProbe] noise emitted next to " + enemy.name);
            }
            if (Time.time < QuitAt)
                return;
            Debug.Log("[BuildProbe] summary: enemies=" + stats.Enemies + " transitions=" + _transitions + " frames=" + _measured +
                      " averageMs=" + (_measured > 0 ? _milliseconds / _measured : 0d).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) +
                      " allocatedBytes=" + _allocated);
            Application.Quit(0);
            enabled = false;
        }

        #endregion
    }
}
