using UnityEngine;
using UnityEngine.Events;

namespace Nekuzaky.HorrorAI
{
    internal sealed partial class HorrorBrain
    {
        #region Warm-up

        private static bool _warmedUp;

        internal static bool WarmedUp => _warmedUp;

        internal static void WarmUp(BehaviorProfile profile)
        {
            if (_warmedUp || profile == null)
                return;
            _warmedUp = true;
            var brain = new HorrorBrain(new IdleMover(), profile, null, 1u);
            var mind = new HorrorMind();
            brain.Begin(0f);
            mind.Memory.Remember(Vector3.forward, 0f, 1f, MemorySource.Hearing);
            mind.NoisesHeard = 1;
            for (var state = 0; state <= (int)HorrorState.LostTrack; state++)
            {
                mind.Suspicion = 1f;
                mind.Perception.Sees = state == (int)HorrorState.Chase;
                brain.Force(mind, (HorrorState)state, TransitionReason.Scripted, brain._now);
                for (var frame = 0; frame < 4; frame++)
                    brain.Think(mind, profile.searchPointWait + 0.1f, brain._now + profile.searchPointWait + 0.1f);
            }
            brain.Force(mind, HorrorState.Search, TransitionReason.CouldNotReach, brain._now);
            var plain = new UnityEvent();
            plain.Invoke();
            plain.AddListener(Ignore);
            plain.Invoke();
            var typed = new UnityEvent<HorrorState>();
            typed.Invoke(HorrorState.Patrol);
            typed.AddListener(Ignore);
            typed.Invoke(HorrorState.Patrol);
        }

        private static void Ignore()
        {
        }

        private static void Ignore(HorrorState state)
        {
        }

        private sealed class IdleMover : IHorrorMover
        {
            public Vector3 Position => Vector3.zero;

            public Vector3 Forward => Vector3.forward;

            public Vector3 Destination => Vector3.zero;

            public bool IsMoving => false;

            public bool Reachable => true;

            public void MoveTo(Vector3 destination, float speed)
            {
            }

            public void Stop()
            {
            }

            public bool HasArrived(float tolerance) => true;

            public bool TryProject(Vector3 point, float maxDistance, out Vector3 projected)
            {
                projected = point;
                return true;
            }

            public void Turn(Vector3 lookAt, float degreesPerSecond, float deltaTime)
            {
            }

            public void Tick(float deltaTime)
            {
            }
        }

        #endregion
    }
}
