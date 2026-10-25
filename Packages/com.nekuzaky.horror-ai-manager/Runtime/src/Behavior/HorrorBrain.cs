using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    internal sealed class HorrorMind
    {
        #region Public

        public float Suspicion;
        public HorrorMemory Memory;
        public HorrorPerception Perception;
        public float SinceStimulus = float.MaxValue;
        public int NoisesHeard;
        public Vector3 TargetPosition;

        public bool Spotted => Perception.Sees && Suspicion >= 0.999f;

        public void Clear()
        {
            Suspicion = 0f;
            Memory = default;
            Perception = default;
            SinceStimulus = float.MaxValue;
            TargetPosition = default;
        }

        public void Forget()
        {
            Suspicion = 0f;
            Memory.Forget();
            Perception.Sees = false;
            Perception.VisibleFraction = 0f;
        }

        #endregion
    }

    internal sealed partial class HorrorBrain
    {
        #region Public

        public const int TrailLength = 6;
        public const float RouteRecheck = 0.5f;

        public HorrorBrain(IHorrorMover mover, BehaviorProfile profile, PatrolRoute route, uint seed)
        {
            _mover = mover;
            Profile = profile;
            Route = route;
            _random = new HorrorRandom(seed);
            Home = mover.Position;
            HomeForward = mover.Forward;
        }

        public BehaviorProfile Profile;
        public PatrolRoute Route;
        public Vector3 Home;
        public Vector3 HomeForward;

        public HorrorState State { get; private set; } = HorrorState.Patrol;

        public HorrorTransition LastTransition { get; private set; }

        public float StateTime => _now - _enteredAt;

        public Vector3 SearchCenter { get; private set; }

        public int PatrolIndex => _patrolIndex;

        public bool Caught { get; private set; }

        public IHorrorMover Mover => _mover;

        public readonly Vector3[] Trail = new Vector3[TrailLength];

        public int TrailCount { get; private set; }

        public bool Think(HorrorMind mind, float deltaTime, float now)
        {
            _now = now;
            _changed = false;
            Caught = false;
            switch (State)
            {
                case HorrorState.Patrol: Patrol(mind, deltaTime); break;
                case HorrorState.Alert: Alert(mind, deltaTime); break;
                case HorrorState.Investigate: Investigate(mind); break;
                case HorrorState.Search: Search(mind, deltaTime); break;
                case HorrorState.Chase: Chase(mind); break;
                case HorrorState.LostTrack: LostTrack(mind); break;
            }
            _mover.Tick(deltaTime);
            return _changed;
        }

        public void Begin(float now)
        {
            _now = now;
            _enteredAt = now;
            State = HorrorState.Patrol;
            LastTransition = new HorrorTransition(HorrorState.Patrol, HorrorState.Patrol, TransitionReason.Started, now);
        }

        public void Force(HorrorMind mind, HorrorState state, TransitionReason reason, float now)
        {
            _now = now;
            Enter(mind, state, reason);
        }

        #endregion


        #region Private and Protected

        private readonly IHorrorMover _mover;
        private HorrorRandom _random;
        private float _now;
        private float _enteredAt;
        private bool _changed;
        private int _patrolIndex;
        private int _patrolDirection = 1;
        private bool _waiting;
        private float _waitLeft;
        private float _lastSeen;
        private float _lastCatch = float.MinValue;
        private int _noisesAtEntry;
        private float _memoryAtEntry;
        private Vector3 _goal;
        private Vector3 _lookAt;

        private void Enter(HorrorMind mind, HorrorState state, TransitionReason reason)
        {
            LastTransition = new HorrorTransition(State, state, reason, _now);
            State = state;
            _enteredAt = _now;
            _changed = true;
            _waiting = false;
            _noisesAtEntry = mind.NoisesHeard;
            _memoryAtEntry = mind.Memory.Time;
            switch (state)
            {
                case HorrorState.Alert:
                    _mover.Stop();
                    break;
                case HorrorState.Search:
                    BeginSearch(mind, reason);
                    break;
                case HorrorState.Chase:
                    _lastSeen = _now;
                    break;
                case HorrorState.Patrol:
                    if (reason == TransitionReason.GaveUp)
                        mind.Forget();
                    _waiting = false;
                    break;
            }
        }

        private bool Spotted(HorrorMind mind, TransitionReason reason)
        {
            if (!mind.Spotted)
                return false;
            Enter(mind, HorrorState.Chase, reason);
            return true;
        }

        private TransitionReason StimulusReason(HorrorMind mind) =>
            mind.Memory.Source == MemorySource.Hearing ? TransitionReason.HeardNoise : TransitionReason.SuspiciousSight;

        private bool NewStimulus(HorrorMind mind) =>
            mind.NoisesHeard != _noisesAtEntry || (mind.Memory.Time > _memoryAtEntry && mind.Memory.Source == MemorySource.Sight);

        #endregion
    }
}
