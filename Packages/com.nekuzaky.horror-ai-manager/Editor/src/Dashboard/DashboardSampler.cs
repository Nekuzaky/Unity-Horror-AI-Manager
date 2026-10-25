using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class FloatRing
    {
        #region Public

        public FloatRing(int capacity) => _values = new float[capacity];

        public int Capacity => _values.Length;

        public int Count => _count;

        public int Version => _version;

        public float this[int oldestFirst] => _values[(_head - _count + oldestFirst + _values.Length * 2) % _values.Length];

        public float Latest => _count > 0 ? this[_count - 1] : 0f;

        public void Push(float value)
        {
            _values[_head] = value;
            _head = (_head + 1) % _values.Length;
            if (_count < _values.Length)
                _count++;
            _version++;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
            _version++;
        }

        public float Max()
        {
            var max = 0f;
            for (var i = 0; i < _count; i++)
                max = Mathf.Max(max, _values[i]);
            return max;
        }

        public float Min()
        {
            var min = 0f;
            for (var i = 0; i < _count; i++)
                min = i == 0 ? _values[i] : Mathf.Min(min, _values[i]);
            return min;
        }

        public float Average()
        {
            if (_count == 0)
                return 0f;
            var sum = 0d;
            for (var i = 0; i < _count; i++)
                sum += _values[i];
            return (float)(sum / _count);
        }

        public float Percentile95(float[] scratch)
        {
            if (_count == 0)
                return 0f;
            var keep = Mathf.Clamp(Mathf.CeilToInt(_count * 0.05f), 1, scratch.Length);
            var kept = 0;
            for (var i = 0; i < _count; i++)
                kept = Insert(scratch, kept, keep, _values[i]);
            return scratch[kept - 1];
        }

        #endregion


        #region Private and Protected

        private readonly float[] _values;
        private int _head;
        private int _count;
        private int _version;

        private static int Insert(float[] top, int kept, int keep, float value)
        {
            if (kept == keep && value <= top[kept - 1])
                return kept;
            var index = kept < keep ? kept : keep - 1;
            for (; index > 0 && top[index - 1] < value; index--)
                top[index] = top[index - 1];
            top[index] = value;
            return kept < keep ? kept + 1 : kept;
        }

        #endregion
    }

    internal struct HearingRecord
    {
        public int Noise;
        public HorrorEnemy Enemy;
        public float Level;
        public int Walls;
        public bool Heard;
    }

    internal sealed class NoiseHearers
    {
        #region Public

        public const int Named = 6;

        public int Sequence { get; private set; } = -1;

        public int HeardCount { get; private set; }

        public int MissedCount { get; private set; }

        public int NamedCount { get; private set; }

        public HearingRecord this[int loudestFirst] => _records[loudestFirst];

        public void Reset(int sequence)
        {
            Sequence = sequence;
            HeardCount = 0;
            MissedCount = 0;
            for (var i = 0; i < NamedCount; i++)
                _records[i] = default;
            NamedCount = 0;
        }

        public void Add(in HearingRecord record)
        {
            if (!record.Heard)
            {
                MissedCount++;
                return;
            }
            HeardCount++;
            if (NamedCount == Named && record.Level <= _records[Named - 1].Level)
                return;
            var index = NamedCount < Named ? NamedCount++ : Named - 1;
            for (; index > 0 && _records[index - 1].Level < record.Level; index--)
                _records[index] = _records[index - 1];
            _records[index] = record;
        }

        #endregion


        #region Private and Protected

        private readonly HearingRecord[] _records = new HearingRecord[Named];

        #endregion
    }

    internal sealed class EnemyTrack
    {
        #region Public

        public const int HistoryLength = 16;

        public HorrorEnemy Enemy { get; private set; }

        public readonly FloatRing Suspicion = new(DashboardSampler.BucketCapacity);

        public int TransitionCount => _count;

        public int Version => _version;

        public HorrorTransition Transition(int newestFirst) => _transitions[(_head - 1 - newestFirst + HistoryLength * 2) % HistoryLength];

        public void Add(HorrorTransition transition)
        {
            _transitions[_head] = transition;
            _head = (_head + 1) % HistoryLength;
            if (_count < HistoryLength)
                _count++;
            _version++;
        }

        public void Reset(HorrorEnemy enemy)
        {
            Enemy = enemy;
            Suspicion.Clear();
            _head = 0;
            _count = 0;
            _version++;
        }

        #endregion


        #region Private and Protected

        private readonly HorrorTransition[] _transitions = new HorrorTransition[HistoryLength];
        private int _head;
        private int _count;
        private int _version;

        #endregion
    }

    internal sealed class DashboardSampler : IHorrorProbeListener
    {
        #region Public

        public const int FrameCapacity = 600;
        public const int BucketCapacity = 300;
        public const float BucketSeconds = 0.1f;
        public const int HearingCapacity = 256;
        public const int TrackLimit = 256;
        public const int Spare = 8;

        public DashboardSampler()
        {
            for (var i = 0; i < Spare; i++)
                _pool.Push(new EnemyTrack());
            for (var i = 0; i < _noises.Length; i++)
                _noises[i] = new NoiseHearers();
        }

        public readonly FloatRing FrameCost = new(FrameCapacity);
        public readonly FloatRing FrameRays = new(FrameCapacity);
        public readonly FloatRing FramePerceptions = new(FrameCapacity);
        public readonly FloatRing FrameAllocated = new(FrameCapacity);
        public readonly FloatRing CostHistory = new(BucketCapacity);
        public readonly FloatRing SuspicionHistory = new(BucketCapacity);

        public int Frames => _frames;

        public int Version => _version;

        public int HearingVersion => _hearingVersion;

        public bool IsAttached => ReferenceEquals(HorrorProbe.Listener, this);

        public float AverageCost { get; private set; }

        public float PeakCost { get; private set; }

        public float AverageRays { get; private set; }

        public float AveragePerceptions { get; private set; }

        public long WorstAllocated { get; private set; } = -1L;

        public void Attach() => HorrorProbe.Listener = this;

        public void Detach()
        {
            if (IsAttached)
                HorrorProbe.Listener = null;
        }

        public void Clear()
        {
            FrameCost.Clear();
            FrameRays.Clear();
            FramePerceptions.Clear();
            FrameAllocated.Clear();
            CostHistory.Clear();
            SuspicionHistory.Clear();
            ReleaseTracks();
            _hearingCount = 0;
            _hearingHead = 0;
            _firstNoise = -1;
            for (var i = 0; i < _noises.Length; i++)
                _noises[i].Reset(-1);
            _bucketCost = 0d;
            _bucketFrames = 0;
            _nextBucket = 0f;
            _world = null;
            _version++;
            _hearingVersion++;
        }

        public EnemyTrack TrackOf(HorrorEnemy enemy) => enemy != null && _tracks.TryGetValue(enemy, out var track) ? track : null;

        public void Summarize()
        {
            AverageCost = FrameCost.Average();
            PeakCost = FrameCost.Percentile95(_scratch);
            AverageRays = FrameRays.Average();
            AveragePerceptions = FramePerceptions.Average();
            WorstAllocated = FrameAllocated.Count == 0 || FrameAllocated.Min() < 0f ? -1L : (long)FrameAllocated.Max();
        }

        public int HearingCount => _hearingCount;

        public HearingRecord Hearing(int newestFirst) => _hearings[(_hearingHead - 1 - newestFirst + HearingCapacity * 2) % HearingCapacity];

        public NoiseHearers HearersOf(int sequence)
        {
            if (sequence < 0)
                return null;
            var hearers = _noises[sequence % HorrorWorld.NoiseCapacity];
            return hearers.Sequence == sequence ? hearers : null;
        }

        public bool Recorded(int sequence) => _firstNoise >= 0 && sequence >= _firstNoise;

        public void Ticked(HorrorWorld world)
        {
            if (!ReferenceEquals(world, _world))
            {
                if (_world != null)
                    Clear();
                _world = world;
                _nextBucket = world.Now + BucketSeconds;
            }
            if (_firstNoise < 0)
                _firstNoise = world.NoiseSequence;
            var stats = world.Stats;
            FrameCost.Push(stats.Milliseconds);
            FrameRays.Push(stats.Raycasts);
            FramePerceptions.Push(stats.Perceptions);
            FrameAllocated.Push(stats.AllocatedBytes);
            _bucketCost += stats.Milliseconds;
            _bucketFrames++;
            _frames++;
            EnsureTracks(world);
            if (world.Now >= _nextBucket)
                CloseBucket(world);
        }

        public void Changed(HorrorEnemy enemy, HorrorTransition transition)
        {
            var track = Track(enemy, false);
            if (track == null)
                return;
            track.Add(transition);
            _version++;
        }

        public void Heard(HorrorEnemy enemy, int noise, float level, int walls, bool heard)
        {
            var record = new HearingRecord { Noise = noise, Enemy = enemy, Level = level, Walls = walls, Heard = heard };
            _hearings[_hearingHead] = record;
            if (noise >= 0)
            {
                var hearers = _noises[noise % HorrorWorld.NoiseCapacity];
                if (hearers.Sequence != noise)
                    hearers.Reset(noise);
                hearers.Add(record);
            }
            _hearingHead = (_hearingHead + 1) % HearingCapacity;
            if (_hearingCount < HearingCapacity)
                _hearingCount++;
            _hearingVersion++;
        }

        #endregion


        #region Private and Protected

        private sealed class ReferenceComparer : IEqualityComparer<HorrorEnemy>
        {
            public bool Equals(HorrorEnemy x, HorrorEnemy y) => ReferenceEquals(x, y);

            public int GetHashCode(HorrorEnemy enemy) => RuntimeHelpers.GetHashCode(enemy);
        }

        private readonly Dictionary<HorrorEnemy, EnemyTrack> _tracks = new(TrackLimit, new ReferenceComparer());
        private readonly List<EnemyTrack> _active = new(TrackLimit);
        private readonly Stack<EnemyTrack> _pool = new(TrackLimit);
        private readonly HearingRecord[] _hearings = new HearingRecord[HearingCapacity];
        private readonly float[] _scratch = new float[FrameCapacity];
        private readonly NoiseHearers[] _noises = new NoiseHearers[HorrorWorld.NoiseCapacity];
        private int _firstNoise = -1;
        private HorrorWorld _world;
        private int _hearingHead;
        private int _hearingCount;
        private int _hearingVersion;
        private double _bucketCost;
        private int _bucketFrames;
        private float _nextBucket;
        private int _frames;
        private int _version;

        private EnemyTrack Track(HorrorEnemy enemy, bool allowNew = true)
        {
            if (enemy == null)
                return null;
            if (_tracks.TryGetValue(enemy, out var track))
                return track;
            if (_active.Count >= TrackLimit)
                return null;
            if (_pool.Count == 0 && !allowNew)
                return null;
            track = _pool.Count > 0 ? _pool.Pop() : new EnemyTrack();
            track.Reset(enemy);
            _tracks.Add(enemy, track);
            _active.Add(track);
            return track;
        }

        private void EnsureTracks(HorrorWorld world)
        {
            var enemies = world.Enemies;
            for (var i = 0; i < enemies.Count; i++)
                Track(enemies[i]);
            for (var i = _pool.Count; i < Spare; i++)
                _pool.Push(new EnemyTrack());
        }

        private void CloseBucket(HorrorWorld world)
        {
            CostHistory.Push(_bucketFrames > 0 ? (float)(_bucketCost / _bucketFrames) : 0f);
            _bucketCost = 0d;
            _bucketFrames = 0;
            _nextBucket = world.Now - _nextBucket > BucketSeconds ? world.Now + BucketSeconds : _nextBucket + BucketSeconds;
            var highest = 0f;
            var enemies = world.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null)
                    continue;
                var suspicion = enemy.Suspicion;
                highest = Mathf.Max(highest, suspicion);
                Track(enemy)?.Suspicion.Push(suspicion);
            }
            SuspicionHistory.Push(highest);
            Prune();
            _version++;
        }

        private void Prune()
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var track = _active[i];
                if (track.Enemy != null)
                    continue;
                _tracks.Remove(track.Enemy);
                _active.RemoveAt(i);
                track.Reset(null);
                _pool.Push(track);
            }
        }

        private void ReleaseTracks()
        {
            for (var i = 0; i < _active.Count; i++)
            {
                _active[i].Reset(null);
                _pool.Push(_active[i]);
            }
            _active.Clear();
            _tracks.Clear();
        }

        #endregion
    }
}
