using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    public enum HorrorLightShape
    {
        Point,
        Spot
    }

    [AddComponentMenu("Horror AI/Horror Light Source")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/components.md#horror-light-source")]
    public sealed class HorrorLightSource : MonoBehaviour
    {
        #region Public

        [Tooltip("The Unity Light this source follows. Its range, spot angle, shape and on/off state are read from it.")]
        public Light linkedLight;
        [Tooltip("Read range, spot angle, shape and on/off from the linked Light. Turn off to set them by hand.")]
        public bool followLight = true;
        public HorrorLightShape shape = HorrorLightShape.Point;
        [Tooltip("Beyond this distance the light does not reveal anything.")]
        [Min(0f)] public float range = 8f;
        [Tooltip("How much a target right next to the light is revealed. 1 is fully lit.")]
        [Min(0f)] public float intensity = 1f;
        [Tooltip("Full width of the beam, in degrees.")]
        [Range(1f, 179f)] public float spotAngle = 60f;
        [Tooltip("A flashlight or lantern held by a target: when it is on, its holder can be seen from afar.")]
        public bool revealsHolder;
        [Tooltip("Exposure of the holder while this light is on.")]
        [Range(0f, 1f)] public float holderExposure = 0.7f;

        public bool On
        {
            get => _on && (!followLight || linkedLight == null || linkedLight.enabled);
            set => SetOn(value);
        }

        public bool IsOn => enabled && gameObject.activeInHierarchy && On && Intensity > 0f;

        public float Range => followLight && linkedLight != null ? linkedLight.range : range;

        public float SpotAngle => followLight && linkedLight != null ? linkedLight.spotAngle : spotAngle;

        public bool IsSpot => followLight && linkedLight != null ? linkedLight.type == LightType.Spot : shape == HorrorLightShape.Spot;

        public float Intensity => intensity;

        public Vector3 Position => transform.position;

        public HorrorTarget Holder
        {
            get
            {
                if (!_holderResolved)
                {
                    _holder = GetComponentInParent<HorrorTarget>();
                    _holderResolved = true;
                }
                return _holder;
            }
        }

        public void SetOn(bool value)
        {
            _on = value;
            if (followLight && linkedLight != null)
                linkedLight.enabled = value;
        }

        public void Toggle() => SetOn(!On);

        public float ContributionAt(Vector3 point) =>
            LightMath.Contribution(transform.position, transform.forward, IsSpot, SpotAngle, Range, Intensity, point);

        #endregion


        #region Private and Protected

        [SerializeField] private bool _on = true;
        private HorrorTarget _holder;
        private bool _holderResolved;

        private void Reset()
        {
            linkedLight = GetComponent<Light>();
            if (linkedLight == null)
                return;
            range = linkedLight.range;
            spotAngle = linkedLight.spotAngle;
            shape = linkedLight.type == LightType.Spot ? HorrorLightShape.Spot : HorrorLightShape.Point;
        }

        private void OnEnable()
        {
            _holderResolved = false;
            if (Application.isPlaying)
                HorrorAI.World.Add(this);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                HorrorAI.World.Remove(this);
        }

        private void OnTransformParentChanged() => _holderResolved = false;

        #endregion
    }
}
