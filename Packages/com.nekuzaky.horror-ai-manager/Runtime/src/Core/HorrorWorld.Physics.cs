using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    internal sealed partial class HorrorWorld
    {
        #region Public

        public bool CanSee(Vector3 from, Vector3 to, LayerMask occluders, Transform target)
        {
            _raysLeft--;
            _rays++;
            if (!Physics.Linecast(from, to, out var hit, occluders, QueryTriggerInteraction.Ignore))
                return true;
            return target != null && hit.transform.IsChildOf(target);
        }

        public int CountWalls(Vector3 from, Vector3 to, LayerMask occluders, Transform listener, Transform source)
        {
            _raysLeft--;
            _rays++;
            var offset = to - from;
            var distance = offset.magnitude;
            if (distance < 1e-4f)
                return 0;
            var count = Physics.RaycastNonAlloc(new Ray(from, offset / distance), _hits, distance, occluders, QueryTriggerInteraction.Ignore);
            var walls = 0;
            for (var i = 0; i < count && i < _hits.Length; i++)
            {
                var hit = _hits[i].transform;
                if (listener != null && hit.IsChildOf(listener))
                    continue;
                if (source != null && hit.IsChildOf(source))
                    continue;
                walls++;
            }
            return walls;
        }

        public float ExposureOf(HorrorTarget target) => target != null ? target.ExposureValue : 0f;

        public float ComputeExposure(HorrorTarget target, LayerMask occluders)
        {
            var point = target.ExposurePoint;
            var sum = 0f;
            var revealed = 0f;
            var rays = 0;
            var count = Lights.Count;
            for (var i = 0; i < count; i++)
            {
                var light = Lights[i];
                if (light == null || !light.IsOn)
                    continue;
                if (light.Holder == target)
                {
                    if (light.revealsHolder)
                        revealed = Mathf.Max(revealed, light.holderExposure);
                    continue;
                }
                var contribution = light.ContributionAt(point);
                if (contribution <= 0.005f || rays >= MaxLightRaysPerTarget)
                    continue;
                rays++;
                if (CanSee(light.Position, point, occluders, target.transform))
                    sum += contribution;
            }
            return Mathf.Max(LightMath.Exposure(_ambient, sum), revealed);
        }

        #endregion


        #region Private and Protected

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private void UpdateExposures()
        {
            var count = Targets.Count;
            for (var i = 0; i < count; i++)
            {
                var target = Targets[i];
                if (target == null || _now < target.NextExposureTime)
                    continue;
                if (_raysLeft < MaxLightRaysPerTarget)
                {
                    _deferred++;
                    continue;
                }
                target.ExposureValue = ComputeExposure(target, target.occluders);
                target.NextExposureTime = _now + _exposureInterval;
            }
        }

        #endregion
    }
}
