using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    internal sealed partial class HorrorWorld
    {
        #region Public

        public const int MinimumBudget = 24;
        public const int MaxTargetsPerEnemy = 4;
        public const int MaxPointsPerTarget = 4;
        public const int MaxNoisesPerPerception = 4;
        public const int MaxLightRaysPerTarget = 8;
        public const int NoiseCapacity = 64;

        public readonly List<HorrorEnemy> Enemies = new(64);
        public readonly List<HorrorTarget> Targets = new(4);
        public readonly List<HorrorLightSource> Lights = new(64);

        public float Now => _now;

        public HorrorAIStats Stats => _stats;

        public int RaycastBudget
        {
            get => _budget;
            set => _budget = Mathf.Clamp(value, MinimumBudget, 100000);
        }

        public float AmbientLight
        {
            get => _ambient;
            set => _ambient = Mathf.Clamp01(value);
        }

        public float ExposureInterval
        {
            get => _exposureInterval;
            set => _exposureInterval = Mathf.Clamp(value, 0f, 1f);
        }

        public int RaysLeft => _raysLeft;

        public int NoiseSequence => _noiseSequence;

        public void Add(HorrorEnemy enemy)
        {
            if (enemy == null || Enemies.Contains(enemy))
                return;
            _pendingRemovals.Remove(enemy);
            enemy.EnsureInitialized();
            Precompiler.Run();
            HorrorBrain.WarmUp(enemy.BehaviorProfile);
            WarmUpQueries();
            var interval = enemy.PerceptionProfile.updateInterval;
            var spread = (Enemies.Count * 0.618034f) % 1f;
            enemy.Attach(this, _now + interval * spread, _noiseSequence);
            Enemies.Add(enemy);
        }

        public void Remove(HorrorEnemy enemy)
        {
            if (_ticking)
            {
                if (!_pendingRemovals.Contains(enemy))
                    _pendingRemovals.Add(enemy);
                return;
            }
            var index = Enemies.IndexOf(enemy);
            if (index < 0)
                return;
            Enemies.RemoveAt(index);
            if (_cursor > index)
                _cursor--;
            enemy.Detach();
        }

        public void Add(HorrorTarget target)
        {
            if (target == null || Targets.Contains(target))
                return;
            target.RefreshColliders();
            Targets.Add(target);
        }

        public void Remove(HorrorTarget target) => Targets.Remove(target);

        public void Add(HorrorLightSource light)
        {
            if (light != null && !Lights.Contains(light))
                Lights.Add(light);
        }

        public void Remove(HorrorLightSource light) => Lights.Remove(light);

        public void EmitNoise(Vector3 position, float loudness, GameObject source)
        {
            if (loudness <= 0f)
                return;
            _noises[_noiseSequence % NoiseCapacity] = new HorrorNoise(position, loudness, source, _now);
            _noiseSequence++;
        }

        public bool TryGetNoise(int sequence, out HorrorNoise noise)
        {
            if (sequence < 0 || sequence >= _noiseSequence || sequence < _noiseSequence - NoiseCapacity)
            {
                noise = default;
                return false;
            }
            noise = _noises[sequence % NoiseCapacity];
            return true;
        }

        public void Tick(float deltaTime)
        {
            var started = Stopwatch.GetTimestamp();
            GcCounter.Mark(out var allocatedBefore, out var allocationsBefore);
            _ticking = true;
            _now += Mathf.Max(0f, deltaTime);
            _raysLeft = _budget;
            _rays = 0;
            _perceptions = 0;
            _deferred = 0;

            UpdateExposures();
            PerceiveDue();
            var count = Enemies.Count;
            for (var i = 0; i < count; i++)
                Enemies[i].Think(deltaTime, _now);

            _ticking = false;
            FlushRemovals();
            var elapsed = (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
            _stats = new HorrorAIStats(Enemies.Count, Targets.Count, Lights.Count, _perceptions, _rays, _deferred, (float)elapsed,
                GcCounter.Since(allocatedBefore, allocationsBefore), _stats.Frame + 1);
            HorrorProbe.Ticked(this);
        }

        #endregion


        #region Private and Protected

        private readonly HorrorNoise[] _noises = new HorrorNoise[NoiseCapacity];
        private readonly List<HorrorEnemy> _pendingRemovals = new(8);
        private int _noiseSequence;
        private float _now;
        private int _budget = 128;
        private float _ambient = 0.04f;
        private float _exposureInterval = 0.1f;
        private int _raysLeft;
        private int _rays;
        private int _perceptions;
        private int _deferred;
        private int _cursor;
        private bool _ticking;
        private HorrorAIStats _stats;

        private void PerceiveDue()
        {
            var count = Enemies.Count;
            if (count == 0)
                return;
            if (_cursor >= count)
                _cursor = 0;
            var start = _cursor;
            var firstDeferred = -1;
            for (var k = 0; k < count; k++)
            {
                var index = (start + k) % count;
                var enemy = Enemies[index];
                if (!enemy.PerceptionDue(_now))
                    continue;
                if (enemy.EstimateRays(this) > _raysLeft)
                {
                    _deferred++;
                    if (firstDeferred < 0)
                        firstDeferred = index;
                    continue;
                }
                enemy.Perceive(this, _now);
                _perceptions++;
                _cursor = (index + 1) % count;
            }
            if (firstDeferred >= 0)
                _cursor = firstDeferred;
        }

        private void FlushRemovals()
        {
            var count = _pendingRemovals.Count;
            for (var i = 0; i < count; i++)
                Remove(_pendingRemovals[i]);
            _pendingRemovals.Clear();
        }

        #endregion
    }

    public readonly struct HorrorAIStats
    {
        public readonly int Enemies;
        public readonly int Targets;
        public readonly int Lights;
        public readonly int Perceptions;
        public readonly int Raycasts;
        public readonly int Deferred;
        public readonly float Milliseconds;
        public readonly long AllocatedBytes;
        public readonly int Frame;

        public HorrorAIStats(int enemies, int targets, int lights, int perceptions, int raycasts, int deferred, float milliseconds,
            long allocatedBytes, int frame)
        {
            Enemies = enemies;
            Targets = targets;
            Lights = lights;
            Perceptions = perceptions;
            Raycasts = raycasts;
            Deferred = deferred;
            Milliseconds = milliseconds;
            AllocatedBytes = allocatedBytes;
            Frame = frame;
        }
    }
}
