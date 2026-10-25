using Nekuzaky.HorrorAI.Demo;
using UnityEngine;
using UnityEngine.AI;

namespace Nekuzaky.HorrorAI.Dev
{
    internal sealed class DemoDirector
    {
        #region Public

        public enum Phase
        {
            Patrol,
            Knock,
            WaitSearch,
            Approach,
            Flee,
            Hold,
            Done
        }

        public static readonly Vector3 Hidden = new(-2.5f, 0.05f, -13f);
        public static readonly Vector3 UnderLamp = new(-7.6f, 0.05f, -2.4f);
        public static readonly Vector3[] Escape =
        {
            new(8.5f, 0.05f, -1.5f), new(7f, 0.05f, 8.5f), new(-2f, 0.05f, 8.6f), new(-13.2f, 0.05f, 13.2f)
        };

        public Phase Current { get; private set; }

        public HorrorEnemy Enemy { get; private set; }

        public bool Start()
        {
            Enemy = Object.FindAnyObjectByType<HorrorEnemy>();
            var player = Object.FindAnyObjectByType<DemoPlayer>();
            _bottle = GameObject.Find("Bottle 1");
            if (Enemy == null || player == null || _bottle == null)
                return false;
            player.enabled = false;
            for (var i = 0; i < Enemy.onCaught.GetPersistentEventCount(); i++)
                Enemy.onCaught.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            _player = player.transform;
            _controller = player.GetComponent<CharacterController>();
            _target = player.GetComponent<HorrorTarget>();
            if (player.flashlight != null)
                player.flashlight.SetOn(false);
            Place(Hidden);
            _target.Crouching = true;
            Enter(Phase.Patrol);
            return true;
        }

        public void Step(float deltaTime)
        {
            if (_controller != null && _controller.enabled)
                _controller.enabled = false;
            var elapsed = Time.time - _since;
            switch (Current)
            {
                case Phase.Patrol:
                    if (elapsed > 3f)
                        Enter(Phase.Knock);
                    break;
                case Phase.Knock:
                    Knock();
                    Enter(Phase.WaitSearch);
                    break;
                case Phase.WaitSearch:
                    if (Enemy.State == HorrorState.Search || elapsed > 14f)
                        Go(Phase.Approach, false, UnderLamp);
                    break;
                case Phase.Approach:
                    Walk(deltaTime, 2.6f, 4f);
                    if (Enemy.State == HorrorState.Chase || elapsed > 10f)
                        Go(Phase.Flee, false, Escape);
                    break;
                default:
                    Late(deltaTime, elapsed);
                    break;
            }
        }

        #endregion


        #region Private and Protected

        private Transform _player;
        private CharacterController _controller;
        private HorrorTarget _target;
        private GameObject _bottle;
        private Vector3[] _path = new Vector3[0];
        private int _corner;
        private float _since;
        private float _walked;

        private void Late(float deltaTime, float elapsed)
        {
            if (Current == Phase.Done)
                return;
            if (Walk(deltaTime, 7.5f, 3f))
                _target.Crouching = true;
            if (Current == Phase.Flee && (Enemy.State == HorrorState.LostTrack || elapsed > 14f))
                Enter(Phase.Hold);
            else if (Current == Phase.Hold && elapsed > 4.5f)
                Enter(Phase.Done);
        }


        private void Enter(Phase phase)
        {
            Current = phase;
            _since = Time.time;
            Debug.Log("[Director] " + phase + " at " + Time.time.ToString("0.0") + " s, enemy " + Enemy.State);
        }

        private void Go(Phase phase, bool crouch, params Vector3[] destinations)
        {
            _target.Crouching = crouch;
            var corners = new System.Collections.Generic.List<Vector3> { _player.position };
            var path = new NavMeshPath();
            for (var i = 0; i < destinations.Length; i++)
            {
                var from = corners[corners.Count - 1];
                if (NavMesh.CalculatePath(from, destinations[i], NavMesh.AllAreas, path) && path.corners.Length > 1)
                    corners.AddRange(path.corners[1..]);
                else
                    corners.Add(destinations[i]);
            }
            _path = corners.ToArray();
            _corner = 1;
            Enter(phase);
        }

        private bool Walk(float deltaTime, float speed, float loudness)
        {
            if (_corner >= _path.Length)
                return true;
            var position = _player.position;
            var goal = _path[_corner];
            goal.y = position.y;
            var step = Vector3.MoveTowards(position, goal, speed * deltaTime);
            var moved = Vector3.Distance(position, step);
            if (moved > 1e-4f)
                _player.rotation = Quaternion.LookRotation(goal - position);
            _player.position = step;
            _walked += moved;
            if (_walked > 1.6f)
            {
                _walked = 0f;
                HorrorAI.EmitNoise(step, _target.Crouching ? 1.2f : loudness, _player.gameObject);
            }
            if (Vector3.Distance(step, goal) < 0.05f)
                _corner++;
            return _corner >= _path.Length;
        }

        private void Knock()
        {
            if (_bottle.TryGetComponent(out Rigidbody body))
                body.AddForce(new Vector3(-1.5f, 0.5f, 0.4f), ForceMode.VelocityChange);
            if (_bottle.TryGetComponent(out NoiseEmitter emitter))
                emitter.EmitLoudness(14f);
            else
                HorrorAI.EmitNoise(_bottle.transform.position, 14f, _bottle);
        }

        private void Place(Vector3 position)
        {
            if (_controller != null)
                _controller.enabled = false;
            _player.position = position;
            _player.rotation = Quaternion.Euler(0f, -60f, 0f);
        }

        #endregion
    }
}
