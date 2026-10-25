using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    public static class SightMath
    {
        #region Main API

        public static bool InCone(Vector3 eye, Vector3 forward, Vector3 point, float viewAngle, float range, float proximityRadius)
        {
            var offset = point - eye;
            var squared = offset.sqrMagnitude;
            if (squared <= proximityRadius * proximityRadius)
                return true;
            if (squared > range * range)
                return false;
            if (squared < 1e-8f)
                return true;
            var cos = Vector3.Dot(forward.normalized, offset / Mathf.Sqrt(squared));
            return cos >= Mathf.Cos(Mathf.Clamp(viewAngle, 0f, 360f) * 0.5f * Mathf.Deg2Rad);
        }

        public static float LightFactor(float exposure, float darkGain) => Mathf.Lerp(Mathf.Clamp01(darkGain), 1f, Mathf.Clamp01(exposure));

        public static float DistanceFactor(float distance, float range, float closeRange, float farGain)
        {
            if (distance <= closeRange || range <= closeRange)
                return 1f;
            var t = Mathf.Clamp01((distance - closeRange) / (range - closeRange));
            return Mathf.Lerp(1f, Mathf.Clamp01(farGain), t);
        }

        public static float GainRate(float sightGain, float visibleFraction, float exposure, float darkGain, float distance, float range,
            float closeRange, float farGain, float visibility)
        {
            if (visibleFraction <= 0f || visibility <= 0f)
                return 0f;
            return Mathf.Max(0f, sightGain) * Mathf.Clamp01(visibleFraction) * LightFactor(exposure, darkGain) *
                   DistanceFactor(distance, range, closeRange, farGain) * visibility;
        }

        public static float Integrate(float suspicion, float gainRate, float decayRate, float decayDelay, float sinceSeen, float deltaTime)
        {
            if (gainRate > 0f)
                return Mathf.Clamp01(suspicion + gainRate * deltaTime);
            var decaying = Mathf.Min(deltaTime, Mathf.Max(0f, sinceSeen - decayDelay));
            return Mathf.Clamp01(suspicion - Mathf.Max(0f, decayRate) * decaying);
        }

        #endregion
    }

    public static class LightMath
    {
        #region Main API

        public static float Falloff(float distance, float range)
        {
            if (range <= 0f || distance >= range)
                return 0f;
            var ratio = distance / range;
            var t = Mathf.Clamp01(1f - ratio * ratio);
            return t * t;
        }

        public static float SpotFactor(Vector3 lightForward, Vector3 toTarget, float spotAngle)
        {
            var length = toTarget.magnitude;
            if (length < 1e-5f)
                return 1f;
            var cos = Vector3.Dot(lightForward.normalized, toTarget / length);
            var outer = Mathf.Cos(Mathf.Clamp(spotAngle, 1f, 179f) * 0.5f * Mathf.Deg2Rad);
            var inner = Mathf.Cos(Mathf.Clamp(spotAngle, 1f, 179f) * 0.4f * Mathf.Deg2Rad);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(outer, inner, cos));
        }

        public static float Contribution(Vector3 lightPosition, Vector3 lightForward, bool spot, float spotAngle, float range, float intensity,
            Vector3 point)
        {
            var toTarget = point - lightPosition;
            var falloff = Falloff(toTarget.magnitude, range);
            if (falloff <= 0f)
                return 0f;
            var cone = spot ? SpotFactor(lightForward, toTarget, spotAngle) : 1f;
            return Mathf.Max(0f, intensity) * falloff * cone;
        }

        public static float Exposure(float ambient, float lightSum) => Mathf.Clamp01(Mathf.Max(0f, ambient) + Mathf.Max(0f, lightSum));

        #endregion
    }

    public static class HearingMath
    {
        #region Main API

        public static float Perceived(float loudness, float distance, float hearingMultiplier, int walls, float wallDamping)
        {
            var reach = Mathf.Max(0f, loudness) * Mathf.Max(0f, hearingMultiplier);
            if (reach <= 0f || distance >= reach)
                return 0f;
            var level = 1f - distance / reach;
            var damping = Mathf.Clamp01(wallDamping);
            for (var i = 0; i < walls && i < 8; i++)
                level *= damping;
            return Mathf.Clamp01(level);
        }

        public static float Reach(float loudness, float hearingMultiplier) => Mathf.Max(0f, loudness) * Mathf.Max(0f, hearingMultiplier);

        public static float FromSound(float volume, float maxDistance, float scale, float fallbackDistance)
        {
            var distance = maxDistance > 0f ? maxDistance : fallbackDistance;
            return Mathf.Max(0f, volume) * Mathf.Max(0f, distance) * Mathf.Max(0f, scale);
        }

        #endregion
    }

    public struct HorrorRandom
    {
        #region Public

        public HorrorRandom(uint seed)
        {
            _state = seed == 0u ? 0x9E3779B9u : seed;
        }

        public float Next01()
        {
            var x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x == 0u ? 0x9E3779B9u : x;
            return (_state >> 8) * (1f / 16777216f);
        }

        public Vector3 InsideDisc(Vector3 center, float radius)
        {
            var angle = Next01() * Mathf.PI * 2f;
            var distance = Mathf.Sqrt(Next01()) * Mathf.Max(0f, radius);
            return center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }

        #endregion


        #region Private and Protected

        private uint _state;

        #endregion
    }
}
