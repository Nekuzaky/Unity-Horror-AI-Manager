using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    internal sealed partial class HorrorWorld
    {
        #region Public

        public const float SourceRadius = 0.25f;

        public bool CanSee(Vector3 from, Vector3 to, LayerMask occluders, ColliderSet owner)
        {
            _raysLeft--;
            _rays++;
            if (!Physics.Linecast(from, to, out var hit, occluders, QueryTriggerInteraction.Ignore))
                return true;
            return owner != null && owner.Contains(hit.colliderEntityId);
        }

        public int CountWalls(Vector3 from, Vector3 to, LayerMask occluders, ColliderSet listener, ColliderSet source, bool hasSource)
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
                var id = _hits[i].colliderEntityId;
                if (listener != null && listener.Contains(id))
                    continue;
                if (source != null && source.Contains(id))
                    continue;
                if (hasSource && _hits[i].distance < SourceRadius)
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
                if (CanSee(light.RayOrigin(point), point, occluders, target.Colliders))
                    sum += contribution;
            }
            return Mathf.Max(LightMath.Exposure(_ambient, sum), revealed);
        }

        #endregion


        #region Private and Protected

        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private static bool _queriesWarmedUp;

        private void WarmUpQueries()
        {
            if (_queriesWarmedUp)
                return;
            _queriesWarmedUp = true;
            var from = new Vector3(0f, -100000f, 0f);
            Physics.Linecast(from, from + Vector3.down, out _, 0, QueryTriggerInteraction.Ignore);
            Physics.RaycastNonAlloc(new Ray(from, Vector3.down), _hits, 1f, 0, QueryTriggerInteraction.Ignore);
            UnityEngine.AI.NavMesh.SamplePosition(from, out _, 0.01f, 0);
            Quaternion.RotateTowards(Quaternion.identity, Quaternion.LookRotation(Vector3.right), 1f);
        }

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
