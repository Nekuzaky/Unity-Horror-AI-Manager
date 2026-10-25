using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    public sealed partial class HorrorEnemy
    {
        #region Perception

        internal int EstimateRays(HorrorWorld world)
        {
            var targets = Mathf.Min(world.Targets.Count, HorrorWorld.MaxTargetsPerEnemy);
            var noises = Mathf.Min(world.NoiseSequence - _noiseCursor, HorrorWorld.MaxNoisesPerPerception);
            return targets * HorrorWorld.MaxPointsPerTarget + Mathf.Max(0, noises);
        }

        internal void Perceive(HorrorWorld world, float now)
        {
            var profile = PerceptionProfile;
            var deltaTime = Mathf.Clamp(now - _lastPerception, 0f, 1f);
            _lastPerception = now;
            _nextPerception += profile.updateInterval;
            if (_nextPerception <= now)
                _nextPerception = now + profile.updateInterval * 0.5f;
            _mind.SinceStimulus += deltaTime;

            Look(world, profile, now, deltaTime);
            Listen(world, profile, now);
            _mind.Perception.LastUpdate = now;
        }

        private void Look(HorrorWorld world, PerceptionProfile profile, float now, float deltaTime)
        {
            var eyePosition = EyePosition;
            var forward = EyeForward;
            HorrorTarget best = null;
            var bestRate = 0f;
            var bestFraction = 0f;
            var bestDistance = 0f;
            var count = world.Targets.Count;
            for (var i = 0; i < count && i < HorrorWorld.MaxTargetsPerEnemy; i++)
            {
                var target = world.Targets[i];
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
                    continue;
                var center = target.Center;
                if (!SightMath.InCone(eyePosition, forward, center, profile.viewAngle, profile.viewRange, profile.proximityRadius))
                    continue;
                var fraction = VisibleFraction(world, profile, target, eyePosition);
                var distance = Vector3.Distance(eyePosition, center);
                var rate = SightMath.GainRate(profile.sightGain, fraction, world.ExposureOf(target), profile.darkGain, distance,
                    profile.viewRange, profile.closeRange, profile.farGain, target.Visibility);
                if (best != null && rate <= bestRate)
                    continue;
                best = target;
                bestRate = rate;
                bestFraction = fraction;
                bestDistance = distance;
            }
            Apply(world, profile, best, bestRate, bestFraction, bestDistance, now, deltaTime);
        }

        private float VisibleFraction(HorrorWorld world, PerceptionProfile profile, HorrorTarget target, Vector3 eyePosition)
        {
            var points = target.SampleCount;
            var visible = 0;
            var root = target.transform;
            for (var p = 0; p < points; p++)
            {
                if (world.CanSee(eyePosition, target.GetSamplePoint(p), profile.occluders, root))
                    visible++;
            }
            return points > 0 ? (float)visible / points : 0f;
        }

        private void Apply(HorrorWorld world, PerceptionProfile profile, HorrorTarget target, float rate, float fraction, float distance,
            float now, float deltaTime)
        {
            var sees = target != null && fraction > 0f && rate > 0f;
            if (target != null || _mind.Perception.Target == null)
                _mind.Perception.Target = target;
            _mind.Perception.Sees = sees;
            _mind.Perception.VisibleFraction = sees ? fraction : 0f;
            _mind.Perception.Distance = target != null ? distance : 0f;
            _mind.Perception.Exposure = target != null ? world.ExposureOf(target) : _mind.Perception.Exposure;
            _mind.Perception.GainRate = sees ? rate : 0f;
            if (sees)
                _mind.SinceStimulus = 0f;
            _mind.Suspicion = SightMath.Integrate(_mind.Suspicion, sees ? rate : 0f, profile.suspicionDecay, profile.decayDelay,
                _mind.SinceStimulus, deltaTime);
            if (!sees)
                return;
            _mind.TargetPosition = target.transform.position;
            _mind.Memory.Remember(target.transform.position, now, _mind.Suspicion, MemorySource.Sight);
        }

        private void Listen(HorrorWorld world, PerceptionProfile profile, float now)
        {
            var end = world.NoiseSequence;
            if (_noiseCursor < end - HorrorWorld.NoiseCapacity)
                _noiseCursor = end - HorrorWorld.NoiseCapacity;
            var ear = EyePosition;
            for (var processed = 0; _noiseCursor < end && processed < HorrorWorld.MaxNoisesPerPerception; processed++)
            {
                var sequence = _noiseCursor++;
                if (!world.TryGetNoise(sequence, out var noise))
                    continue;
                if (noise.Source != null && noise.Source.transform.IsChildOf(transform))
                    continue;
                var distance = Vector3.Distance(noise.Position, ear);
                if (distance >= HearingMath.Reach(noise.Loudness, profile.hearingMultiplier))
                    continue;
                var walls = world.CountWalls(noise.Position, ear, profile.occluders, transform, noise.Source != null ? noise.Source.transform : null);
                var level = HearingMath.Perceived(noise.Loudness, distance, profile.hearingMultiplier, walls, profile.wallDamping);
                if (level >= profile.hearingThreshold)
                    Hear(profile, noise, level, now);
            }
        }

        private void Hear(PerceptionProfile profile, HorrorNoise noise, float level, float now)
        {
            if (_mind.Suspicion < profile.hearingCap)
                _mind.Suspicion = Mathf.Min(profile.hearingCap, _mind.Suspicion + level * profile.hearingGain);
            _mind.SinceStimulus = 0f;
            var tracking = _mind.Memory.Source == MemorySource.Sight && now - _mind.Memory.Time < 1.5f;
            if (!tracking)
                _mind.Memory.Remember(noise.Position, now, level, MemorySource.Hearing);
            _mind.NoisesHeard++;
            _mind.Perception.HeardLoudness = level;
            _mind.Perception.HeardPosition = noise.Position;
            _mind.Perception.HeardTime = now;
            NoiseHeard?.Invoke(this, noise);
            onNoiseHeard?.Invoke();
        }

        #endregion
    }
}
