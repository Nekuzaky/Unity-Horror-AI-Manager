using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    [CreateAssetMenu(menuName = "Horror AI/Perception Profile", fileName = "Perception Profile", order = 1)]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/perception.md")]
    public sealed class PerceptionProfile : ScriptableObject
    {
        #region Public

        [Header("Sight")]
        [Tooltip("Width of the view cone, in degrees.")]
        [Range(1f, 360f)] public float viewAngle = 110f;
        [Tooltip("How far the enemy can see, in meters.")]
        [Min(0.1f)] public float viewRange = 18f;
        [Tooltip("Height of the eyes above the enemy's pivot, used when no Eye transform is set.")]
        [Min(0f)] public float eyeHeight = 1.7f;
        [Tooltip("Inside this radius the enemy notices the target in any direction, as if it felt it behind its back.")]
        [Min(0f)] public float proximityRadius = 1.2f;
        [Tooltip("Within this distance the suspicion gauge fills at full speed.")]
        [Min(0f)] public float closeRange = 3f;
        [Tooltip("Suspicion per second when the target is fully visible, close and fully lit. 2 fills the gauge in half a second.")]
        [Min(0f)] public float sightGain = 2f;
        [Tooltip("How much of that speed is left when the target stands in total darkness (0 = invisible, 1 = light does not matter).")]
        [Range(0f, 1f)] public float darkGain = 0.12f;
        [Tooltip("How much of that speed is left at the edge of the view range.")]
        [Range(0f, 1f)] public float farGain = 0.35f;
        [Tooltip("Suspicion lost per second once the target is out of sight.")]
        [Min(0f)] public float suspicionDecay = 0.2f;
        [Tooltip("Seconds the gauge holds before it starts to decay.")]
        [Min(0f)] public float decayDelay = 1.5f;
        [Tooltip("What blocks sight, light and sound. Leave Everything unless your triggers or glass are on their own layer.")]
        public LayerMask occluders = ~0;

        [Header("Hearing")]
        [Tooltip("Multiplies the loudness of every noise. 2 hears twice as far.")]
        [Min(0f)] public float hearingMultiplier = 1f;
        [Tooltip("Each wall between the noise and the enemy multiplies what it hears by this value.")]
        [Range(0f, 1f)] public float wallDamping = 0.45f;
        [Tooltip("Noises perceived below this level are ignored.")]
        [Range(0f, 1f)] public float hearingThreshold = 0.05f;
        [Tooltip("Suspicion added by a noise heard at full level.")]
        [Min(0f)] public float hearingGain = 0.8f;
        [Tooltip("Noises alone never raise suspicion above this value: hearing makes it investigate, only sight makes it chase.")]
        [Range(0f, 1f)] public float hearingCap = 0.9f;

        [Header("Memory")]
        [Tooltip("Certainty about the last known position lost per second.")]
        [Min(0f)] public float memoryDecay = 0.04f;

        [Header("Performance")]
        [Tooltip("Seconds between two perception updates. 0.1 is ten times per second; enemies are spread over frames.")]
        [Range(0.02f, 1f)] public float updateInterval = 0.1f;

        #endregion
    }
}
