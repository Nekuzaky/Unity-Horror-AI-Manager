using System;
using System.Collections.Generic;
using Nekuzaky.Resonance;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Integrations
{
    [CreateAssetMenu(menuName = "Horror AI/Resonance Noise Map", fileName = "Resonance Noise Map", order = 20)]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/integrations.md")]
    public sealed class ResonanceNoiseMap : ScriptableObject
    {
        #region Public

        [Serializable]
        public struct Entry
        {
            [Tooltip("The Sound Event this line is about.")]
            public SoundEvent sound;
            [Tooltip("Enemies never hear this sound (music, UI, the enemy's own voice).")]
            public bool silent;
            [Tooltip("Loudness for the AI, in meters: how far the sound carries in open space.")]
            [Min(0f)] public float loudness;
        }

        [Tooltip("Sounds with their own loudness for the AI. Sounds not listed use the default below.")]
        public List<Entry> entries = new();
        [Tooltip("Sounds not listed are heard with a loudness derived from their volume and attenuation max distance.")]
        public bool useDefault = true;
        [Tooltip("Default loudness = volume x max distance x this scale. 0.5 makes a full-volume sound that fades out at 20 m heard up to 10 m.")]
        [Min(0f)] public float defaultScale = 0.5f;
        [Tooltip("Sounds quieter than this are ignored by default.")]
        [Range(0f, 1f)] public float minimumVolume = 0.05f;

        public bool TryGetLoudness(SoundEvent sound, float volume, float maxDistance, out float loudness)
        {
            Build();
            if (sound != null && _lookup.TryGetValue(sound, out var entry))
            {
                loudness = entry.silent ? 0f : entry.loudness;
                return loudness > 0f;
            }
            return Default(useDefault, defaultScale, minimumVolume, volume, maxDistance, out loudness);
        }

        public static bool Default(bool enabled, float scale, float minimumVolume, float volume, float maxDistance, out float loudness)
        {
            loudness = 0f;
            if (!enabled || maxDistance <= 0f || volume < minimumVolume)
                return false;
            loudness = HearingMath.FromSound(volume, maxDistance, scale, 0f);
            return loudness > 0f;
        }

        public void Rebuild() => _built = false;

        #endregion


        #region Private and Protected

        private readonly Dictionary<SoundEvent, Entry> _lookup = new();
        private bool _built;

        private void OnEnable() => _built = false;

        private void OnValidate() => _built = false;

        private void Build()
        {
            if (_built)
                return;
            _lookup.Clear();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.sound != null)
                        _lookup[entry.sound] = entry;
                }
            }
            _built = true;
        }

        #endregion
    }
}
