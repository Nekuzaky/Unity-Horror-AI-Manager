using Nekuzaky.Resonance;
using UnityEngine;
using ResonanceApi = Nekuzaky.Resonance.Resonance;

namespace Nekuzaky.HorrorAI.Integrations
{
    [AddComponentMenu("Horror AI/Resonance Noise Bridge")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/integrations.md")]
    public sealed class ResonanceNoiseBridge : MonoBehaviour
    {
        #region Public

        [Tooltip("Which Sound Events enemies hear, and how loud. Without a map, every 3D sound is heard with a loudness derived from its volume and max distance.")]
        public ResonanceNoiseMap map;
        [Tooltip("Used when no map is set: loudness = volume x max distance x this scale.")]
        [Min(0f)] public float defaultScale = 0.5f;
        [Tooltip("Used when no map is set: quieter sounds are ignored.")]
        [Range(0f, 1f)] public float minimumVolume = 0.05f;

        public int NoisesSent => _sent;

        public bool TryGetLoudness(SoundEvent sound, float volume, float maxDistance, out float loudness)
        {
            if (map != null)
                return map.TryGetLoudness(sound, volume, maxDistance, out loudness);
            return ResonanceNoiseMap.Default(true, defaultScale, minimumVolume, volume, maxDistance, out loudness);
        }

        #endregion


        #region Private and Protected

        private ResonanceEngine _engine;
        private int _sent;

        internal void OnSoundStarted(SoundStartInfo info)
        {
            if (!TryGetLoudness(info.Event, info.Volume, info.MaxDistance, out var loudness))
                return;
            _sent++;
            HorrorAI.EmitNoise(info.Position, loudness, info.Emitter);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;
            _engine = ResonanceApi.Engine;
            if (_engine != null)
                _engine.SoundStarted += OnSoundStarted;
        }

        private void OnDisable()
        {
            if (_engine != null)
                _engine.SoundStarted -= OnSoundStarted;
            _engine = null;
        }

        #endregion
    }
}
