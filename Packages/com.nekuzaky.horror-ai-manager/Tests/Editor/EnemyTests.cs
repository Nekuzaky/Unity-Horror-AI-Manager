using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class EnemyTests : Fixture
    {
        [Test]
        public void FullCycle_SpotChaseLoseSearchGiveUp_RaisesEveryEvent()
        {
            var archetype = Archetype(out _, out var behavior);
            behavior.searchDuration = 2f;
            behavior.lostTrackTimeout = 1f;
            var enemy = Enemy(Vector3.zero, Vector3.forward, archetype);
            var target = Target(new Vector3(0f, 0f, 14f));
            Lamp(new Vector3(0f, 2.5f, 14.5f), 6f);
            var fired = new List<string>();
            enemy.onSpotted.AddListener(() => fired.Add("spotted"));
            enemy.onChaseStarted.AddListener(() => fired.Add("chase"));
            enemy.onLostTrack.AddListener(() => fired.Add("lost"));
            enemy.onSearchStarted.AddListener(() => fired.Add("search"));
            enemy.onGaveUp.AddListener(() => fired.Add("gave up"));
            var states = new List<HorrorState>();
            enemy.StateChanged += (_, transition) => states.Add(transition.To);

            Assert.Less(SecondsUntil(() => enemy.State == HorrorState.Chase, 5f), 5f);
            World.Remove(target);
            Assert.Less(SecondsUntil(() => enemy.State == HorrorState.Patrol, 15f), 15f);

            Assert.AreEqual(new List<string> { "spotted", "chase", "lost", "search", "gave up" }, Without(fired, "chase", 1));
            Assert.Contains(HorrorState.LostTrack, states);
            Assert.AreEqual(TransitionReason.GaveUp, enemy.LastTransition.Reason);
        }

        [Test]
        public void GlobalStateChanged_ReportsTheEnemyAndTheReason()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            HorrorEnemy reported = null;
            var reason = TransitionReason.None;
            void Listener(HorrorEnemy e, HorrorTransition t)
            {
                reported = e;
                reason = t.Reason;
            }
            HorrorAI.StateChanged += Listener;
            try
            {
                HorrorAI.EmitNoise(new Vector3(3f, 0f, 3f), 15f);
                Tick(20);
            }
            finally
            {
                HorrorAI.StateChanged -= Listener;
            }

            Assert.AreSame(enemy, reported);
            Assert.AreEqual(TransitionReason.HeardNoise, reason);
            Assert.AreEqual(HorrorState.Alert, enemy.State);
        }

        [Test]
        public void Investigate_FromAScriptSendsItToThePosition()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            var investigated = 0;
            enemy.onInvestigate.AddListener(() => investigated++);

            enemy.Investigate(new Vector3(0f, 0f, 3f));
            Tick(120);

            Assert.AreEqual(1, investigated);
            Assert.AreEqual(HorrorState.Search, enemy.State);
            Assert.AreEqual(MemorySource.Scripted, enemy.Memory.Source);
        }

        [Test]
        public void Calm_FromAScriptForgetsEverything()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            enemy.Investigate(new Vector3(0f, 0f, 3f));

            enemy.Calm();

            Assert.AreEqual(HorrorState.Patrol, enemy.State);
            Assert.AreEqual(0f, enemy.Suspicion);
            Assert.IsFalse(enemy.Memory.HasMemory);
        }

        [Test]
        public void Caught_IsRaisedWhenTheChaserReachesTheTarget()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            Target(new Vector3(0f, 0f, 2.5f));
            Lamp(new Vector3(0f, 2.5f, 3f), 6f);
            HorrorTarget caught = null;
            enemy.Caught += (_, target) => caught = target;
            var unityCaught = 0;
            enemy.onCaught.AddListener(() => unityCaught++);

            Tick(240);

            Assert.IsNotNull(caught);
            Assert.Greater(unityCaught, 0);
        }

        [Test]
        public void Memory_DecaysWhileNothingHappens()
        {
            var archetype = Archetype(out var perception, out _);
            perception.memoryDecay = 0.5f;
            var enemy = Enemy(Vector3.zero, Vector3.forward, archetype);
            HorrorAI.EmitNoise(new Vector3(0f, 0f, -2f), 10f);
            Tick(6);
            var certainty = enemy.Memory.Certainty;

            Tick(60);

            Assert.Greater(certainty, 0f);
            Assert.AreEqual(Mathf.Max(0f, certainty - 0.5f), enemy.Memory.Certainty, 0.05f);
        }

        [Test]
        public void RemovingAnEnemyDuringItsOwnEventIsSafe()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            var other = Enemy(new Vector3(2f, 0f, 0f), Vector3.forward, Archetype(out _, out _));
            enemy.onAlerted.AddListener(() => World.Remove(enemy));

            HorrorAI.EmitNoise(new Vector3(1f, 0f, 3f), 15f);
            Tick(30);

            Assert.IsFalse(World.Enemies.Contains(enemy));
            Assert.IsTrue(World.Enemies.Contains(other));
            Assert.IsFalse(enemy.IsActive);
        }

        [Test]
        public void EyePosition_IsMeasuredFromTheFeetOfANavMeshAgent()
        {
            var enemy = Enemy(new Vector3(0f, 1f, 0f), Vector3.forward, Archetype(out var perception, out _), false);
            var agent = enemy.gameObject.AddComponent<NavMeshAgent>();
            agent.baseOffset = 1f;

            Assert.AreEqual(perception.eyeHeight, enemy.EyePosition.y, 1e-4f);
            Assert.AreEqual(0f, enemy.FeetPosition.y, 1e-4f);
        }

        [Test]
        public void EyePosition_WithoutAnAgentStartsAtThePivot()
        {
            var enemy = Enemy(new Vector3(0f, 0.5f, 0f), Vector3.forward, Archetype(out var perception, out _), false);

            Assert.AreEqual(0.5f + perception.eyeHeight, enemy.EyePosition.y, 1e-4f);
        }

        [Test]
        public void DisablingAChasingEnemy_SendsItBackToPatrol()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            Target(new Vector3(0f, 0f, 4f));
            Lamp(new Vector3(0f, 2.5f, 4.5f), 6f);
            Assert.Less(SecondsUntil(() => enemy.State == HorrorState.Chase, 5f), 5f);
            var changes = new List<HorrorTransition>();
            enemy.StateChanged += (_, transition) => changes.Add(transition);

            World.Remove(enemy);

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(HorrorState.Chase, changes[0].From);
            Assert.AreEqual(HorrorState.Patrol, changes[0].To);
            Assert.AreEqual(TransitionReason.Disabled, changes[0].Reason);
            Assert.AreEqual(HorrorState.Patrol, enemy.State);
        }

        [Test]
        public void ReenablingAnEnemy_StartsFromAClearMind()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            var target = Target(new Vector3(0f, 0f, 4f));
            Lamp(new Vector3(0f, 2.5f, 4.5f), 6f);
            Assert.Less(SecondsUntil(() => enemy.State == HorrorState.Chase, 5f), 5f);
            World.Remove(enemy);
            target.transform.position = new Vector3(0f, 0f, -40f);
            var changes = new List<HorrorTransition>();
            enemy.StateChanged += (_, transition) => changes.Add(transition);

            World.Add(enemy);
            Tick(2);

            Assert.IsEmpty(changes);
            Assert.AreEqual(HorrorState.Patrol, enemy.State);
            Assert.AreEqual(0f, enemy.Suspicion);
            Assert.IsFalse(enemy.Memory.HasMemory);
            Assert.IsFalse(enemy.Perception.Sees);
        }

        private static List<string> Without(List<string> events, string value, int keep)
        {
            var result = new List<string>();
            var seen = 0;
            foreach (var item in events)
            {
                if (item == value && ++seen > keep)
                    continue;
                result.Add(item);
            }
            return result;
        }
    }
}
