using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class BehaviorTests : Fixture
    {
        #region Private and Protected

        private HorrorBrain _brain;
        private HorrorMind _mind;
        private BehaviorProfile _profile;
        private Transform _body;
        private float _now;
        private readonly List<HorrorTransition> _transitions = new();

        private void Create(PatrolRoute route = null, uint seed = 7u, IHorrorMover mover = null)
        {
            _body = Track(new GameObject("Body")).transform;
            _profile = Asset<BehaviorProfile>();
            _mind = new HorrorMind();
            _brain = new HorrorBrain(mover ?? new TransformMover(_body), _profile, route, seed);
            _brain.Begin(0f);
            _now = 0f;
            _transitions.Clear();
        }

        private void Run(float seconds)
        {
            var frames = Mathf.CeilToInt(seconds / Frame);
            for (var i = 0; i < frames; i++)
            {
                _now += Frame;
                if (_brain.Think(_mind, Frame, _now))
                    _transitions.Add(_brain.LastTransition);
            }
        }

        private void Force(HorrorState state)
        {
            _brain.Force(_mind, state, TransitionReason.Scripted, _now);
            _transitions.Clear();
        }

        private void Remember(Vector3 position, MemorySource source, float suspicion)
        {
            _mind.Memory.Remember(position, _now + 0.001f, 1f, source);
            _mind.Suspicion = suspicion;
            if (source == MemorySource.Hearing)
                _mind.NoisesHeard++;
        }

        private void See(Vector3 position)
        {
            _mind.Perception.Sees = true;
            _mind.TargetPosition = position;
            _mind.Suspicion = 1f;
            _mind.Memory.Remember(position, _now + 0.001f, 1f, MemorySource.Sight);
        }

        private void Blind()
        {
            _mind.Perception.Sees = false;
        }

        private HorrorTransition Last => _transitions[_transitions.Count - 1];

        private PatrolRoute Route(float wait, params Vector3[] points)
        {
            var route = Track(new GameObject("Route")).AddComponent<PatrolRoute>();
            foreach (var point in points)
                Track(route.AddWaypoint(point, wait).gameObject);
            return route;
        }

        #endregion


        #region Patrol

        [Test]
        public void Patrol_WalksTheRouteAndWaitsAtEachWaypoint()
        {
            Create(Route(1f, Vector3.zero, new Vector3(0f, 0f, 3f), new Vector3(3f, 0f, 3f)));

            Run(0.5f);
            Assert.AreEqual(0, _brain.PatrolIndex);
            Run(1f);
            Assert.AreEqual(1, _brain.PatrolIndex);
            Run(4.5f);
            Assert.AreEqual(2, _brain.PatrolIndex);
            Assert.Less(Vector3.Distance(_body.position, new Vector3(3f, 0f, 3f)), _profile.arriveDistance);
            Assert.AreEqual(HorrorState.Patrol, _brain.State);
            Assert.IsEmpty(_transitions);
        }

        [Test]
        public void Patrol_OneWaypointKeepsWatchingForNewOnes()
        {
            var route = Route(0.5f, new Vector3(0f, 0f, 2f));
            Create(route);
            Run(3f);
            Assert.AreEqual(0, _brain.PatrolIndex);
            Assert.Less(Vector3.Distance(_body.position, new Vector3(0f, 0f, 2f)), _profile.arriveDistance);

            Track(route.AddWaypoint(new Vector3(3f, 0f, 2f), 0.5f).gameObject);
            Run(1.5f);

            Assert.AreEqual(1, _brain.PatrolIndex);
            Assert.IsEmpty(_transitions);
        }

        [Test]
        public void Patrol_WaypointsAddedAtTheEndOfAOnceRouteAreFollowed()
        {
            var route = Route(0.2f, Vector3.zero, new Vector3(0f, 0f, 2f));
            route.mode = PatrolMode.Once;
            Create(route);
            Run(4f);
            Assert.AreEqual(1, _brain.PatrolIndex);

            Track(route.AddWaypoint(new Vector3(2f, 0f, 2f), 0.2f).gameObject);
            Run(2.5f);

            Assert.AreEqual(2, _brain.PatrolIndex);
        }

        [Test]
        public void Patrol_WithoutARouteGuardsItsHome()
        {
            Create();
            _body.position = new Vector3(4f, 0f, 0f);

            Run(5f);

            Assert.Less(Vector3.Distance(_body.position, Vector3.zero), _profile.arriveDistance + 0.01f);
        }

        [Test]
        public void Patrol_BecomesAlertWhenItGlimpsesSomething()
        {
            Create();
            Remember(new Vector3(0f, 0f, 5f), MemorySource.Sight, _profile.alertThreshold);

            Run(Frame);

            Assert.AreEqual(HorrorState.Alert, _brain.State);
            Assert.AreEqual(TransitionReason.SuspiciousSight, Last.Reason);
            Assert.AreEqual(HorrorState.Patrol, Last.From);
        }

        [Test]
        public void Patrol_BecomesAlertWhenItHearsANoise()
        {
            Create();
            Remember(new Vector3(0f, 0f, -5f), MemorySource.Hearing, 0.5f);

            Run(Frame);

            Assert.AreEqual(HorrorState.Alert, _brain.State);
            Assert.AreEqual(TransitionReason.HeardNoise, Last.Reason);
        }

        [Test]
        public void Patrol_StaysCalmBelowTheAlertThreshold()
        {
            Create();
            Remember(new Vector3(0f, 0f, 5f), MemorySource.Sight, _profile.alertThreshold * 0.9f);

            Run(1f);

            Assert.AreEqual(HorrorState.Patrol, _brain.State);
        }

        #endregion


        #region Alert and Investigate

        [Test]
        public void Alert_StaresThenGoesToInvestigate()
        {
            Create();
            Remember(new Vector3(0f, 0f, 5f), MemorySource.Hearing, 0.5f);
            Run(Frame);

            Run(_profile.alertDuration * 0.5f);
            Assert.AreEqual(HorrorState.Alert, _brain.State);
            Run(_profile.alertDuration * 0.6f);

            Assert.AreEqual(HorrorState.Investigate, _brain.State);
            Assert.AreEqual(TransitionReason.AlertOver, Last.Reason);
        }

        [Test]
        public void Alert_CalmsDownWhenNothingIsLeftToCheck()
        {
            Create();
            Remember(new Vector3(0f, 0f, 5f), MemorySource.Sight, 0.5f);
            Run(Frame);
            _mind.Suspicion = 0f;
            _mind.Memory.Forget();

            Run(_profile.alertDuration + 0.1f);

            Assert.AreEqual(HorrorState.Patrol, _brain.State);
            Assert.AreEqual(TransitionReason.CalmedDown, Last.Reason);
        }

        [Test]
        public void Investigate_ReachesTheSpotThenSearches()
        {
            Create();
            Remember(new Vector3(0f, 0f, 4f), MemorySource.Hearing, 0.5f);
            Force(HorrorState.Investigate);

            Run(4f);

            Assert.AreEqual(HorrorState.Search, _brain.State);
            Assert.AreEqual(TransitionReason.ReachedLastKnownPosition, Last.Reason);
            Assert.Less(Vector3.Distance(_brain.SearchCenter, new Vector3(0f, 0f, 4f)), 0.01f);
        }

        [Test]
        public void Investigate_SearchesFromWhereItIsWhenItTakesTooLong()
        {
            Create();
            _profile.investigateTimeout = 1f;
            Remember(new Vector3(0f, 0f, 100f), MemorySource.Hearing, 0.5f);
            Force(HorrorState.Investigate);

            Run(1.1f);

            Assert.AreEqual(HorrorState.Search, _brain.State);
            Assert.AreEqual(TransitionReason.InvestigationTimedOut, Last.Reason);
        }

        [Test]
        public void Investigate_AnUnreachableSpotIsSearchedFromTheNearestReachablePoint()
        {
            var mover = new BlockedMover(new Vector3(2f, 0f, 0f));
            Create(null, 7u, mover);
            Remember(new Vector3(10f, 0f, 10f), MemorySource.Hearing, 0.5f);
            Force(HorrorState.Investigate);

            Run(0.5f);

            Assert.AreEqual(HorrorState.Search, _brain.State);
            Assert.AreEqual(TransitionReason.CouldNotReach, Last.Reason);
            StringAssert.StartsWith("could not reach", Last.Explanation);
            Assert.Less(Vector3.Distance(_brain.SearchCenter, new Vector3(2f, 0f, 0f)), 0.01f);
        }

        [Test]
        public void LostTrack_AnUnreachableSpotIsSearchedFromTheNearestReachablePoint()
        {
            var mover = new BlockedMover(new Vector3(-3f, 0f, 1f));
            Create(null, 7u, mover);
            See(new Vector3(0f, 0f, 2f));
            Force(HorrorState.Chase);
            Blind();
            _mind.Memory.Remember(new Vector3(20f, 0f, 20f), _now + 0.01f, 1f, MemorySource.Sight);

            Run(_profile.loseSightGrace + 0.5f);

            Assert.AreEqual(HorrorState.Search, _brain.State);
            Assert.AreEqual(TransitionReason.CouldNotReach, Last.Reason);
            Assert.Less(Vector3.Distance(_brain.SearchCenter, new Vector3(-3f, 0f, 1f)), 0.01f);
        }

        private sealed class BlockedMover : IHorrorMover
        {
            public BlockedMover(Vector3 nearest)
            {
                Position = nearest;
            }

            public Vector3 Position { get; }

            public Vector3 Forward => Vector3.forward;

            public Vector3 Destination { get; private set; }

            public bool IsMoving => false;

            public bool Reachable => false;

            public void MoveTo(Vector3 destination, float speed) => Destination = destination;

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


        #region Chase

        [TestCase(HorrorState.Patrol)]
        [TestCase(HorrorState.Alert)]
        [TestCase(HorrorState.Investigate)]
        [TestCase(HorrorState.Search)]
        public void Spotted_StartsAChaseFromAnyCalmState(HorrorState state)
        {
            Create();
            Remember(new Vector3(0f, 0f, 3f), MemorySource.Hearing, 0.5f);
            Force(state);

            See(new Vector3(0f, 0f, 6f));
            Run(Frame);

            Assert.AreEqual(HorrorState.Chase, _brain.State);
            Assert.AreEqual(TransitionReason.Spotted, Last.Reason);
            Assert.AreEqual(state, Last.From);
        }

        [Test]
        public void Chase_RunsAtTheTargetAndCatchesItWithACooldown()
        {
            Create();
            See(new Vector3(0f, 0f, 6f));
            Run(Frame);
            var caught = 0;

            for (var i = 0; i < 240; i++)
            {
                Run(Frame);
                if (_brain.Caught)
                    caught++;
            }

            Assert.AreEqual(HorrorState.Chase, _brain.State);
            Assert.Less(Vector3.Distance(_body.position, new Vector3(0f, 0f, 6f)), _profile.catchDistance);
            Assert.AreEqual(2, caught);
        }

        [Test]
        public void Chase_LosesTrackAfterTheGraceWithoutSight()
        {
            Create();
            See(new Vector3(0f, 0f, 30f));
            Run(Frame);
            Blind();

            Run(_profile.loseSightGrace * 0.8f);
            Assert.AreEqual(HorrorState.Chase, _brain.State);
            Run(_profile.loseSightGrace * 0.3f);

            Assert.AreEqual(HorrorState.LostTrack, _brain.State);
            Assert.AreEqual(TransitionReason.LostSight, Last.Reason);
        }

        [Test]
        public void LostTrack_ChasesAgainWhenTheTargetReappears()
        {
            Create();
            Remember(new Vector3(0f, 0f, 20f), MemorySource.Sight, 0.6f);
            Force(HorrorState.LostTrack);

            See(new Vector3(0f, 0f, 8f));
            Run(Frame);

            Assert.AreEqual(HorrorState.Chase, _brain.State);
            Assert.AreEqual(TransitionReason.Reacquired, Last.Reason);
        }

        [Test]
        public void LostTrack_SearchesWhenItReachesTheLastKnownPosition()
        {
            Create();
            Remember(new Vector3(0f, 0f, 3f), MemorySource.Sight, 0.6f);
            Force(HorrorState.LostTrack);

            Run(2f);

            Assert.AreEqual(HorrorState.Search, _brain.State);
            Assert.AreEqual(TransitionReason.ReachedLastKnownPosition, Last.Reason);
        }

        [Test]
        public void LostTrack_SearchesWhenItTakesTooLong()
        {
            Create();
            _profile.lostTrackTimeout = 1f;
            Remember(new Vector3(0f, 0f, 200f), MemorySource.Sight, 0.6f);
            Force(HorrorState.LostTrack);

            Run(1.1f);

            Assert.AreEqual(HorrorState.Search, _brain.State);
            Assert.AreEqual(TransitionReason.LostTrackOver, Last.Reason);
        }

        #endregion


        #region Search

        [Test]
        public void Search_ANewNoiseSendsItToInvestigate()
        {
            Create();
            Remember(Vector3.zero, MemorySource.Hearing, 0.5f);
            Force(HorrorState.Search);
            Run(1f);

            Remember(new Vector3(6f, 0f, 0f), MemorySource.Hearing, 0.6f);
            Run(Frame);

            Assert.AreEqual(HorrorState.Investigate, _brain.State);
            Assert.AreEqual(TransitionReason.HeardNoise, Last.Reason);
        }

        [Test]
        public void Search_GivesUpAfterItsDurationAndForgets()
        {
            Create();
            _profile.searchDuration = 3f;
            Remember(new Vector3(2f, 0f, 2f), MemorySource.Hearing, 0.4f);
            Force(HorrorState.Search);

            Run(3.1f);

            Assert.AreEqual(HorrorState.Patrol, _brain.State);
            Assert.AreEqual(TransitionReason.GaveUp, Last.Reason);
            Assert.AreEqual(0f, _mind.Suspicion);
            Assert.IsFalse(_mind.Memory.HasMemory);
        }

        [Test]
        public void Search_StaysInsideTheRadiusAndIsRepeatableWithASeed()
        {
            var first = SearchPath(11u);
            var second = SearchPath(11u);
            var other = SearchPath(12u);

            Assert.AreEqual(first, second);
            Assert.AreNotEqual(first, other);
        }

        private string SearchPath(uint seed)
        {
            Create(null, seed);
            _profile.searchDuration = 60f;
            _profile.searchPointWait = 0.2f;
            _body.position = new Vector3(10f, 0f, 10f);
            Remember(new Vector3(10f, 0f, 10f), MemorySource.Hearing, 0.4f);
            Force(HorrorState.Search);
            var path = string.Empty;
            for (var i = 0; i < 20; i++)
            {
                Run(0.5f);
                var offset = _body.position - new Vector3(10f, 0f, 10f);
                offset.y = 0f;
                Assert.LessOrEqual(offset.magnitude, _profile.searchRadius + 0.01f);
                path += _body.position.ToString("F2");
            }
            return path;
        }

        #endregion
    }
}
