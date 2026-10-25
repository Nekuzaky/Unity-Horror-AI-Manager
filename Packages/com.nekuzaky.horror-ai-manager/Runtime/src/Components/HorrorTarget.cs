using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    [AddComponentMenu("Horror AI/Horror Target")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/components.md#horror-target")]
    public sealed class HorrorTarget : MonoBehaviour
    {
        #region Public

        [Tooltip("Crouching makes the target harder to see and lowers the points enemies look at, so low cover hides it.")]
        public bool crouching;
        [Tooltip("Multiplies how fast enemies notice this target. 0 makes it invisible, 2 twice as easy to notice.")]
        [Min(0f)] public float visibilityMultiplier = 1f;
        [Tooltip("Visibility while crouching, as a fraction of the standing visibility.")]
        [Range(0f, 1f)] public float crouchVisibility = 0.5f;
        [Tooltip("Height of the sample points while crouching, as a fraction of their standing height.")]
        [Range(0.1f, 1f)] public float crouchHeight = 0.55f;
        [Tooltip("Points on the body, relative to the pivot, that enemies cast a sight ray to (four at most).")]
        public Vector3[] samplePoints = { new(0f, 0.3f, 0f), new(0f, 1.0f, 0f), new(0f, 1.6f, 0f) };
        [Tooltip("What blocks the light that reaches this target.")]
        public LayerMask occluders = ~0;

        public bool Crouching
        {
            get => crouching;
            set => crouching = value;
        }

        public float Visibility => Mathf.Max(0f, visibilityMultiplier) * (crouching ? crouchVisibility : 1f);

        public float Exposure => ExposureValue;

        public int SampleCount => samplePoints == null || samplePoints.Length == 0 ? 1 : Mathf.Min(samplePoints.Length, HorrorWorld.MaxPointsPerTarget);

        public Vector3 GetSamplePoint(int index)
        {
            var t = transform;
            var local = samplePoints == null || samplePoints.Length == 0 ? new Vector3(0f, 1f, 0f) : samplePoints[Mathf.Clamp(index, 0, samplePoints.Length - 1)];
            if (crouching)
                local.y *= crouchHeight;
            return t.position + t.rotation * local;
        }

        public void RefreshColliders() => Colliders.Refresh(this);

        public Vector3 Center
        {
            get
            {
                var height = crouching ? crouchHeight : 1f;
                return transform.position + transform.up * height;
            }
        }

        #endregion


        #region Private and Protected

        internal readonly ColliderSet Colliders = new();
        internal float ExposureValue;
        internal float NextExposureTime;

        internal Vector3 ExposurePoint => Center;

        private void OnEnable()
        {
            if (Application.isPlaying)
                HorrorAI.World.Add(this);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                HorrorAI.World.Remove(this);
        }

        private void OnTransformChildrenChanged() => RefreshColliders();

        #endregion
    }
}
