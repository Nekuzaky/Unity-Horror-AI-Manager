using UnityEngine;
using UnityEngine.AI;

namespace Nekuzaky.HorrorAI
{
    public interface IHorrorMover
    {
        Vector3 Position { get; }

        Vector3 Forward { get; }

        Vector3 Destination { get; }

        bool IsMoving { get; }

        void MoveTo(Vector3 destination, float speed);

        void Stop();

        bool HasArrived(float tolerance);

        bool TryProject(Vector3 point, float maxDistance, out Vector3 projected);

        void Turn(Vector3 lookAt, float degreesPerSecond, float deltaTime);

        void Tick(float deltaTime);
    }

    internal sealed class NavMeshMover : IHorrorMover
    {
        #region Public

        public NavMeshMover(NavMeshAgent agent)
        {
            _agent = agent;
            _transform = agent.transform;
        }

        public Vector3 Position => _transform.position;

        public Vector3 Forward => _transform.forward;

        public Vector3 Destination => _destination;

        public bool IsMoving => _moving;

        public void MoveTo(Vector3 destination, float speed)
        {
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
                return;
            if (!Mathf.Approximately(_agent.speed, speed))
                _agent.speed = speed;
            if (_agent.isStopped)
                _agent.isStopped = false;
            if (_moving && (destination - _destination).sqrMagnitude < RepathDistance * RepathDistance)
                return;
            _destination = destination;
            _moving = _agent.SetDestination(destination);
        }

        public void Stop()
        {
            _moving = false;
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh && !_agent.isStopped)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        public bool HasArrived(float tolerance)
        {
            if (!_moving)
                return true;
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
                return true;
            if (_agent.pathPending)
                return false;
            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                return true;
            return _agent.remainingDistance <= Mathf.Max(tolerance, _agent.stoppingDistance);
        }

        public bool TryProject(Vector3 point, float maxDistance, out Vector3 projected)
        {
            if (NavMesh.SamplePosition(point, out var hit, maxDistance, _agent.areaMask))
            {
                projected = hit.position;
                return true;
            }
            projected = point;
            return false;
        }

        public void Turn(Vector3 lookAt, float degreesPerSecond, float deltaTime) => Rotate(_transform, lookAt, degreesPerSecond, deltaTime);

        public void Tick(float deltaTime)
        {
        }

        internal static void Rotate(Transform transform, Vector3 lookAt, float degreesPerSecond, float deltaTime)
        {
            var direction = lookAt - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f)
                return;
            var target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * deltaTime);
        }

        #endregion


        #region Private and Protected

        private const float RepathDistance = 0.35f;
        private readonly NavMeshAgent _agent;
        private readonly Transform _transform;
        private Vector3 _destination;
        private bool _moving;

        #endregion
    }

    internal sealed class TransformMover : IHorrorMover
    {
        #region Public

        public TransformMover(Transform transform)
        {
            _transform = transform;
            _destination = transform.position;
        }

        public Vector3 Position => _transform.position;

        public Vector3 Forward => _transform.forward;

        public Vector3 Destination => _destination;

        public bool IsMoving => _moving;

        public void MoveTo(Vector3 destination, float speed)
        {
            _destination = destination;
            _speed = speed;
            _moving = true;
        }

        public void Stop() => _moving = false;

        public bool HasArrived(float tolerance)
        {
            if (!_moving)
                return true;
            var offset = _destination - _transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= tolerance * tolerance;
        }

        public bool TryProject(Vector3 point, float maxDistance, out Vector3 projected)
        {
            projected = point;
            return true;
        }

        public void Turn(Vector3 lookAt, float degreesPerSecond, float deltaTime) =>
            NavMeshMover.Rotate(_transform, lookAt, degreesPerSecond, deltaTime);

        public void Tick(float deltaTime)
        {
            if (!_moving)
                return;
            var position = _transform.position;
            var goal = new Vector3(_destination.x, position.y, _destination.z);
            NavMeshMover.Rotate(_transform, goal, 720f, deltaTime);
            _transform.position = Vector3.MoveTowards(position, goal, _speed * deltaTime);
        }

        #endregion


        #region Private and Protected

        private readonly Transform _transform;
        private Vector3 _destination;
        private float _speed;
        private bool _moving;

        #endregion
    }
}
