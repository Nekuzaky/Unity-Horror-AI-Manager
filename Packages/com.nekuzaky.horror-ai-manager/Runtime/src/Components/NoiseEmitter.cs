using System;
using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    [Flags]
    public enum NoiseTrigger
    {
        None = 0,
        CollisionEnter = 1 << 0,
        TriggerEnter = 1 << 1,
        Start = 1 << 2,
        Enable = 1 << 3
    }

    [AddComponentMenu("Horror AI/Noise Emitter")]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/components.md#noise-emitter")]
    public sealed class NoiseEmitter : MonoBehaviour
    {
        #region Public

        [Tooltip("How far the noise carries in open space, in meters, for an enemy with a hearing multiplier of 1.")]
        [Min(0f)] public float loudness = 8f;
        [Tooltip("When the noise is made. Leave None to only make it from an event in the inspector, an animation event or a script.")]
        public NoiseTrigger emitOn = NoiseTrigger.CollisionEnter;
        [Tooltip("Softer impacts make no noise.")]
        [Min(0f)] public float minImpactSpeed = 1.5f;
        [Tooltip("Impacts at this speed or faster make the full noise; slower ones are quieter.")]
        [Min(0f)] public float fullImpactSpeed = 6f;
        [Tooltip("Minimum seconds between two noises, so a rolling object does not shout every frame.")]
        [Min(0f)] public float cooldown = 0.3f;
        [Tooltip("Only react to triggers and collisions with objects that have this tag. Empty reacts to everything.")]
        public string triggerTag = string.Empty;

        public float LastEmitTime => _lastEmit;

        public void Emit() => EmitLoudness(loudness);

        public void EmitLoudness(float value)
        {
            var now = Time.time;
            if (now - _lastEmit < cooldown)
                return;
            _lastEmit = now;
            HorrorAI.EmitNoise(transform.position, value, gameObject);
        }

        public void EmitScaled(float scale) => EmitLoudness(loudness * Mathf.Max(0f, scale));

        public static float ImpactLoudness(float loudness, float speed, float minSpeed, float fullSpeed)
        {
            if (speed < minSpeed)
                return 0f;
            var t = fullSpeed > minSpeed ? Mathf.InverseLerp(minSpeed, fullSpeed, speed) : 1f;
            return Mathf.Max(0f, loudness) * Mathf.Lerp(0.35f, 1f, t);
        }

        #endregion


        #region Private and Protected

        private float _lastEmit = float.MinValue;

        private bool Matches(GameObject other) => string.IsNullOrEmpty(triggerTag) || (other != null && other.CompareTag(triggerTag));

        private void Start()
        {
            if ((emitOn & NoiseTrigger.Start) != 0)
                Emit();
        }

        private void OnEnable()
        {
            if ((emitOn & NoiseTrigger.Enable) != 0 && Application.isPlaying)
                Emit();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if ((emitOn & NoiseTrigger.CollisionEnter) == 0 || !Matches(collision.gameObject))
                return;
            var value = ImpactLoudness(loudness, collision.relativeVelocity.magnitude, minImpactSpeed, fullImpactSpeed);
            if (value > 0f)
                EmitLoudness(value);
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((emitOn & NoiseTrigger.TriggerEnter) != 0 && Matches(other.gameObject))
                Emit();
        }

        #endregion
    }
}
