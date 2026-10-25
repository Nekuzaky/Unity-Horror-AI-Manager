using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Nekuzaky.HorrorAI
{
    [AddComponentMenu("Horror AI/Horror Enemy")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/components.md#horror-enemy")]
    public sealed partial class HorrorEnemy : MonoBehaviour
    {
        #region Public

        [Tooltip("How this enemy sees, hears and behaves. Use the Patroller preset to start.")]
        public HorrorArchetype archetype;
        [Tooltip("The waypoints it walks while nothing is wrong. Without one it guards the spot where it starts.")]
        public PatrolRoute patrolRoute;
        [Tooltip("Optional: the head or eyes. Sight starts here and follows its forward direction.")]
        public Transform eye;
        [Tooltip("Seed of the search pattern, so a run is repeatable.")]
        public int seed = 1;

        public UnityEvent<HorrorState> onStateChanged = new();
        public UnityEvent onAlerted = new();
        public UnityEvent onInvestigate = new();
        public UnityEvent onSpotted = new();
        public UnityEvent onChaseStarted = new();
        public UnityEvent onLostTrack = new();
        public UnityEvent onSearchStarted = new();
        public UnityEvent onGaveUp = new();
        public UnityEvent onCaught = new();
        public UnityEvent onNoiseHeard = new();

        public event Action<HorrorEnemy, HorrorTransition> StateChanged;

        public event Action<HorrorEnemy, HorrorTarget> Caught;

        public event Action<HorrorEnemy, HorrorNoise> NoiseHeard;

        public HorrorState State => _brain != null ? _brain.State : HorrorState.Patrol;

        public HorrorTransition LastTransition => _brain != null ? _brain.LastTransition : default;

        public float StateTime => _brain != null ? _brain.StateTime : 0f;

        public float Suspicion => _mind.Suspicion;

        public HorrorMemory Memory => _mind.Memory;

        public HorrorPerception Perception => _mind.Perception;

        public HorrorTarget Target => _mind.Perception.Target;

        public bool IsActive => _world != null;

        public Vector3 Destination => _brain != null ? _brain.Mover.Destination : transform.position;

        public bool IsMoving => _brain != null && _brain.Mover.IsMoving;

        public Vector3 SearchCenter => _brain != null ? _brain.SearchCenter : transform.position;

        public HorrorArchetype Archetype => archetype != null ? archetype : HorrorArchetype.Default;

        public PerceptionProfile PerceptionProfile => Archetype.Perception;

        public BehaviorProfile BehaviorProfile => Archetype.Behavior;

        public Vector3 EyePosition => eye != null ? eye.position : FeetPosition + Vector3.up * PerceptionProfile.eyeHeight;

        public Vector3 FeetPosition
        {
            get
            {
                if (!_agentResolved || (_agent == null && !Application.isPlaying))
                    ResolveAgent();
                var position = transform.position;
                if (_agent != null)
                    position.y -= _agent.baseOffset * transform.lossyScale.y;
                return position;
            }
        }

        public Vector3 EyeForward => eye != null ? eye.forward : transform.forward;

        public int CopySearchTrail(Vector3[] buffer)
        {
            if (_brain == null || buffer == null)
                return 0;
            var count = Mathf.Min(buffer.Length, _brain.TrailCount);
            for (var i = 0; i < count; i++)
                buffer[i] = _brain.Trail[i];
            return count;
        }

        public void Investigate(Vector3 position)
        {
            EnsureInitialized();
            var now = HorrorAI.World.Now;
            _mind.Memory.Remember(position, now, 1f, MemorySource.Scripted);
            _mind.Suspicion = Mathf.Max(_mind.Suspicion, BehaviorProfile.alertThreshold);
            _brain.Force(_mind, HorrorState.Investigate, TransitionReason.Scripted, now);
            Raise(_brain.LastTransition);
        }

        public void Calm()
        {
            EnsureInitialized();
            _mind.Forget();
            _brain.Force(_mind, HorrorState.Patrol, TransitionReason.Scripted, HorrorAI.World.Now);
            Raise(_brain.LastTransition);
        }

        public void SetMover(IHorrorMover mover)
        {
            if (mover == null)
                return;
            _brain = new HorrorBrain(mover, BehaviorProfile, patrolRoute, (uint)seed);
            _brain.Begin(HorrorAI.World.Now);
        }

        #endregion


        #region Private and Protected

        private readonly HorrorMind _mind = new();
        private readonly ColliderSet _colliders = new();
        private NavMeshAgent _agent;
        private bool _agentResolved;
        private HorrorBrain _brain;
        private HorrorWorld _world;
        private float _nextPerception;
        private float _lastPerception;
        private int _noiseCursor;

        internal HorrorMind Mind => _mind;

        internal HorrorBrain Brain => _brain;

        internal void EnsureInitialized()
        {
            if (_brain != null)
                return;
            ResolveAgent();
            IHorrorMover mover = _agent != null ? new NavMeshMover(_agent) : new TransformMover(transform);
            _brain = new HorrorBrain(mover, BehaviorProfile, patrolRoute, (uint)seed);
            _brain.Begin(HorrorAI.World.Now);
        }

        internal void Attach(HorrorWorld world, float nextPerception, int noiseCursor)
        {
            _mind.Clear();
            _colliders.Refresh(this);
            PrimeEvents();
            _world = world;
            _nextPerception = nextPerception;
            _lastPerception = world.Now;
            _noiseCursor = noiseCursor;
            _brain.Begin(world.Now);
            HorrorAI.RaiseEnemyAdded(this);
        }

        internal void Detach()
        {
            var now = _world != null ? _world.Now : HorrorAI.World.Now;
            _world = null;
            if (_brain != null && _brain.State != HorrorState.Patrol)
            {
                _brain.Force(_mind, HorrorState.Patrol, TransitionReason.Disabled, now);
                Raise(_brain.LastTransition);
            }
            _brain?.Mover.Stop();
            HorrorAI.RaiseEnemyRemoved(this);
        }

        internal void PrimeEvents()
        {
            EventPrimer.Prime(onStateChanged);
            EventPrimer.Prime(onAlerted);
            EventPrimer.Prime(onInvestigate);
            EventPrimer.Prime(onSpotted);
            EventPrimer.Prime(onChaseStarted);
            EventPrimer.Prime(onLostTrack);
            EventPrimer.Prime(onSearchStarted);
            EventPrimer.Prime(onGaveUp);
            EventPrimer.Prime(onCaught);
            EventPrimer.Prime(onNoiseHeard);
        }

                private void ResolveAgent()
        {
            TryGetComponent(out _agent);
            _agentResolved = true;
        }

        internal bool PerceptionDue(float now) => now >= _nextPerception;

        internal void Think(float deltaTime, float now)
        {
            _brain.Profile = BehaviorProfile;
            _brain.Route = patrolRoute;
            _mind.Memory.Decay(PerceptionProfile.memoryDecay, deltaTime);
            var target = _mind.Perception.Target;
            if (_mind.Perception.Sees && target != null)
                _mind.TargetPosition = target.transform.position;
            if (_brain.Think(_mind, deltaTime, now))
                Raise(_brain.LastTransition);
            if (_brain.Caught)
                RaiseCaught(target);
        }

        private void Raise(HorrorTransition transition)
        {
            StateChanged?.Invoke(this, transition);
            HorrorAI.RaiseStateChanged(this, transition);
            onStateChanged?.Invoke(transition.To);
            switch (transition.To)
            {
                case HorrorState.Alert: onAlerted?.Invoke(); break;
                case HorrorState.Investigate: onInvestigate?.Invoke(); break;
                case HorrorState.Search: onSearchStarted?.Invoke(); break;
                case HorrorState.LostTrack: onLostTrack?.Invoke(); break;
                case HorrorState.Chase:
                    if (transition.Reason == TransitionReason.Spotted)
                        onSpotted?.Invoke();
                    onChaseStarted?.Invoke();
                    break;
                case HorrorState.Patrol:
                    if (transition.Reason == TransitionReason.GaveUp)
                        onGaveUp?.Invoke();
                    break;
            }
        }

        private void RaiseCaught(HorrorTarget target)
        {
            Caught?.Invoke(this, target);
            onCaught?.Invoke();
        }

        private void Awake() => EnsureInitialized();

        private void OnTransformChildrenChanged() => _colliders.Refresh(this);

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;
            HorrorAI.World.Add(this);
            HorrorAI.EnsureRunner();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                HorrorAI.World.Remove(this);
        }

        #endregion
    }
}
