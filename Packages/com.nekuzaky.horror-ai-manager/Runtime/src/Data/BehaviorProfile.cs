using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    [CreateAssetMenu(menuName = "Horror AI/Behavior Profile", fileName = "Behavior Profile", order = 2)]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/behavior.md")]
    public sealed class BehaviorProfile : ScriptableObject
    {
        #region Public

        [Header("Thresholds")]
        [Tooltip("Suspicion at which a patrolling enemy stops and becomes alert.")]
        [Range(0.01f, 1f)] public float alertThreshold = 0.3f;
        [Tooltip("Below this suspicion, an alert enemy with nothing to check goes back to its patrol.")]
        [Range(0f, 1f)] public float calmThreshold = 0.1f;

        [Header("Durations (seconds)")]
        [Tooltip("How long the enemy stands still and stares before it goes to check.")]
        [Min(0f)] public float alertDuration = 1.2f;
        [Tooltip("How long it may walk toward the spot it wants to check before searching from where it is.")]
        [Min(0.1f)] public float investigateTimeout = 15f;
        [Tooltip("How long it searches around the last known position before giving up.")]
        [Min(0f)] public float searchDuration = 18f;
        [Tooltip("How long it stops and looks around at each search point.")]
        [Min(0f)] public float searchPointWait = 1.5f;
        [Tooltip("How long a chasing enemy keeps chasing after it stops seeing the target.")]
        [Min(0f)] public float loseSightGrace = 1.5f;
        [Tooltip("How long it rushes to the last place it saw the target before it starts searching.")]
        [Min(0.1f)] public float lostTrackTimeout = 8f;

        [Header("Speeds (m/s)")]
        [Min(0f)] public float patrolSpeed = 1.4f;
        [Min(0f)] public float investigateSpeed = 2.2f;
        [Min(0f)] public float searchSpeed = 1.8f;
        [Min(0f)] public float chaseSpeed = 4.2f;
        [Tooltip("Degrees per second when the enemy turns on the spot.")]
        [Min(1f)] public float turnSpeed = 240f;

        [Header("Search")]
        [Tooltip("Radius around the last known position where it looks for the target.")]
        [Min(0f)] public float searchRadius = 7f;

        [Header("Distances (m)")]
        [Tooltip("Distance at which it counts as arrived at a point.")]
        [Min(0.05f)] public float arriveDistance = 0.6f;
        [Tooltip("Distance at which a chasing enemy catches the target and raises On Caught.")]
        [Min(0f)] public float catchDistance = 1.3f;
        [Tooltip("Seconds between two On Caught events.")]
        [Min(0f)] public float catchCooldown = 2f;

        #endregion
    }
}
