using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    internal sealed partial class HorrorBrain
    {
        #region States

        private void Patrol(HorrorMind mind, float deltaTime)
        {
            if (Spotted(mind, TransitionReason.Spotted))
                return;
            if (mind.Suspicion >= Profile.alertThreshold && mind.Memory.HasMemory)
            {
                Enter(mind, HorrorState.Alert, StimulusReason(mind));
                return;
            }
            if (Route == null || Route.Count == 0)
                GuardHome(deltaTime);
            else
                FollowRoute(deltaTime);
        }

        private void Alert(HorrorMind mind, float deltaTime)
        {
            if (Spotted(mind, TransitionReason.Spotted))
                return;
            if (mind.Memory.HasMemory)
                _mover.Turn(mind.Memory.Position, Profile.turnSpeed, deltaTime);
            if (StateTime < Profile.alertDuration)
                return;
            if (mind.Memory.HasMemory && mind.Suspicion >= Profile.calmThreshold)
                Enter(mind, HorrorState.Investigate, TransitionReason.AlertOver);
            else
                Enter(mind, HorrorState.Patrol, TransitionReason.CalmedDown);
        }

        private void Investigate(HorrorMind mind)
        {
            if (Spotted(mind, TransitionReason.Spotted))
                return;
            _goal = mind.Memory.Position;
            _mover.MoveTo(_goal, Profile.investigateSpeed);
            if (_mover.HasArrived(Profile.arriveDistance))
            {
                Enter(mind, HorrorState.Search, ArrivalReason());
                return;
            }
            if (StateTime >= Profile.investigateTimeout)
                Enter(mind, HorrorState.Search, TransitionReason.InvestigationTimedOut);
        }

        private void Search(HorrorMind mind, float deltaTime)
        {
            if (Spotted(mind, TransitionReason.Spotted))
                return;
            if (NewStimulus(mind) && mind.Suspicion >= Profile.alertThreshold)
            {
                Enter(mind, HorrorState.Investigate, StimulusReason(mind));
                return;
            }
            if (StateTime >= Profile.searchDuration)
            {
                Enter(mind, HorrorState.Patrol, TransitionReason.GaveUp);
                return;
            }
            if (_waiting)
            {
                _waitLeft -= deltaTime;
                _mover.Turn(_lookAt, Profile.turnSpeed * 0.5f, deltaTime);
                if (_waitLeft <= 0f)
                    NextSearchPoint();
                return;
            }
            _mover.MoveTo(_goal, Profile.searchSpeed);
            if (_mover.HasArrived(Profile.arriveDistance))
                WaitAtSearchPoint();
        }

        private void Chase(HorrorMind mind)
        {
            if (mind.Perception.Sees)
            {
                _lastSeen = _now;
                _goal = mind.TargetPosition;
                _mover.MoveTo(_goal, Profile.chaseSpeed);
                var offset = mind.TargetPosition - _mover.Position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= Profile.catchDistance * Profile.catchDistance && _now - _lastCatch >= Profile.catchCooldown)
                {
                    _lastCatch = _now;
                    Caught = true;
                }
                return;
            }
            _goal = mind.Memory.Position;
            _mover.MoveTo(_goal, Profile.chaseSpeed);
            if (_now - _lastSeen >= Profile.loseSightGrace)
                Enter(mind, HorrorState.LostTrack, TransitionReason.LostSight);
        }

        private void LostTrack(HorrorMind mind)
        {
            if (Spotted(mind, TransitionReason.Reacquired))
                return;
            _goal = mind.Memory.Position;
            _mover.MoveTo(_goal, Profile.chaseSpeed);
            if (_mover.HasArrived(Profile.arriveDistance))
                Enter(mind, HorrorState.Search, ArrivalReason());
            else if (StateTime >= Profile.lostTrackTimeout)
                Enter(mind, HorrorState.Search, TransitionReason.LostTrackOver);
        }

        #endregion


        #region Movement

        private void GuardHome(float deltaTime)
        {
            var offset = Home - _mover.Position;
            offset.y = 0f;
            if (offset.sqrMagnitude > Profile.arriveDistance * Profile.arriveDistance)
            {
                _mover.MoveTo(Home, Profile.patrolSpeed);
                return;
            }
            _mover.Stop();
            _mover.Turn(Home + HomeForward, Profile.turnSpeed * 0.5f, deltaTime);
        }

        private void FollowRoute(float deltaTime)
        {
            var count = Route.Count;
            if (_patrolIndex >= count)
                _patrolIndex = 0;
            if (_waiting)
            {
                _waitLeft -= deltaTime;
                if (_waitLeft > 0f)
                    return;
                _waiting = false;
                var next = PatrolRoute.Advance(_patrolIndex, count, Route.mode, ref _patrolDirection);
                if (next == _patrolIndex)
                {
                    _waiting = true;
                    _waitLeft = Mathf.Max(RouteRecheck, Route.GetWait(_patrolIndex));
                    return;
                }
                _patrolIndex = next;
            }
            _goal = Route.GetPosition(_patrolIndex);
            _mover.MoveTo(_goal, Profile.patrolSpeed);
            if (!_mover.HasArrived(Profile.arriveDistance))
                return;
            _mover.Stop();
            _waiting = true;
            _waitLeft = Route.GetWait(_patrolIndex);
        }

        private TransitionReason ArrivalReason() =>
            _mover.Reachable ? TransitionReason.ReachedLastKnownPosition : TransitionReason.CouldNotReach;

        private void BeginSearch(HorrorMind mind, TransitionReason reason)
        {
            var remembered = mind.Memory.HasMemory || mind.Memory.Time > 0f;
            SearchCenter = remembered && reason != TransitionReason.CouldNotReach ? mind.Memory.Position : _mover.Position;
            TrailCount = 0;
            WaitAtSearchPoint();
        }

        private void WaitAtSearchPoint()
        {
            _mover.Stop();
            _waiting = true;
            _waitLeft = Profile.searchPointWait;
            _lookAt = _random.InsideDisc(_mover.Position, 4f);
            if (TrailCount < TrailLength)
                Trail[TrailCount++] = _mover.Position;
            else
                ShiftTrail(_mover.Position);
        }

        private void NextSearchPoint()
        {
            _waiting = false;
            _goal = SearchCenter;
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var candidate = _random.InsideDisc(SearchCenter, Profile.searchRadius);
                if (_mover.TryProject(candidate, 2f, out var projected))
                {
                    _goal = projected;
                    return;
                }
            }
        }

        private void ShiftTrail(Vector3 point)
        {
            for (var i = 1; i < TrailLength; i++)
                Trail[i - 1] = Trail[i];
            Trail[TrailLength - 1] = point;
        }

        #endregion
    }
}
