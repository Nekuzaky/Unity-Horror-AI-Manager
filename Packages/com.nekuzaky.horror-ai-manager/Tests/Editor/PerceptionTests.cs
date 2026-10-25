using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class PerceptionTests : Fixture
    {
        [Test]
        public void Sight_TargetInFrontFillsTheGaugeAndIsRemembered()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            var target = Target(new Vector3(0f, 0f, 6f));

            Tick(30);

            Assert.IsTrue(enemy.Perception.Sees);
            Assert.AreEqual(1f, enemy.Perception.VisibleFraction, 1e-5f);
            Assert.Greater(enemy.Suspicion, 0f);
            Assert.AreSame(target, enemy.Target);
            Assert.AreEqual(MemorySource.Sight, enemy.Memory.Source);
            Assert.Less(Vector3.Distance(enemy.Memory.Position, target.transform.position), 0.01f);
        }

        [Test]
        public void Sight_WallBlocksTheView()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            Target(new Vector3(0f, 0f, 8f));
            Wall(new Vector3(0f, 1.5f, 4f), new Vector3(6f, 3f, 0.3f));

            Tick(60);

            Assert.IsFalse(enemy.Perception.Sees);
            Assert.AreEqual(0f, enemy.Suspicion);
        }

        [Test]
        public void Sight_TargetBehindOrBeyondRangeIsNotSeen()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out var perception, out _));
            Target(new Vector3(0f, 0f, -6f));
            Target(new Vector3(0f, 0f, perception.viewRange + 2f));

            Tick(60);

            Assert.IsFalse(enemy.Perception.Sees);
        }

        [Test]
        public void Sight_LowCoverHidesACrouchingTargetOnly()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            var target = Target(new Vector3(0f, 0f, 8f));
            Wall(new Vector3(0f, 0.6f, 6f), new Vector3(3f, 1.2f, 0.5f));

            Tick(12);
            Assert.IsTrue(enemy.Perception.Sees);
            Assert.AreEqual(1f / 3f, enemy.Perception.VisibleFraction, 1e-4f);

            target.Crouching = true;
            enemy.Mind.Suspicion = 0f;
            Tick(30);
            Assert.IsFalse(enemy.Perception.Sees);
        }

        [Test]
        public void Light_ALitTargetIsSpottedMuchFasterThanOneInTheDark()
        {
            var dark = SpotTime(false);
            var lit = SpotTime(true);

            Assert.Less(lit, 1.5f);
            Assert.Greater(dark, lit * 3f);
        }

        [Test]
        public void Light_AWallBetweenTheLampAndTheTargetKeepsItDark()
        {
            var target = Target(new Vector3(0f, 0f, 0f));
            Lamp(new Vector3(0f, 2f, 3f), 8f);
            Tick(2);
            var lit = target.Exposure;

            Wall(new Vector3(0f, 1.5f, 1.5f), new Vector3(4f, 3f, 0.2f));
            Tick(10);

            Assert.Greater(lit, 0.3f);
            Assert.AreEqual(World.AmbientLight, target.Exposure, 1e-4f);
        }

        [Test]
        public void Light_OffLampsAndOutOfRangeTargetsGiveOnlyAmbient()
        {
            var target = Target(Vector3.zero);
            var lamp = Lamp(new Vector3(0f, 2f, 2f), 6f, false);
            Tick(2);
            Assert.AreEqual(World.AmbientLight, target.Exposure, 1e-4f);

            lamp.SetOn(true);
            lamp.transform.position = new Vector3(0f, 2f, 20f);
            Tick(10);
            Assert.AreEqual(World.AmbientLight, target.Exposure, 1e-4f);
        }

        [Test]
        public void Light_AFlashlightRevealsItsHolder()
        {
            var target = Target(Vector3.zero);
            var go = Track(new GameObject("Flashlight"));
            go.transform.SetParent(target.transform, false);
            var flashlight = go.AddComponent<HorrorLightSource>();
            flashlight.shape = HorrorLightShape.Spot;
            flashlight.revealsHolder = true;
            flashlight.holderExposure = 0.7f;
            World.Add(flashlight);

            Tick(2);
            Assert.AreEqual(0.7f, target.Exposure, 1e-4f);

            flashlight.SetOn(false);
            Tick(10);
            Assert.AreEqual(World.AmbientLight, target.Exposure, 1e-4f);
        }

        [Test]
        public void Hearing_ANoiseInReachRaisesSuspicionAndSetsTheMemory()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out var perception, out _));
            var noise = new Vector3(-4f, 0f, -3f);

            HorrorAI.EmitNoise(noise, 10f);
            Tick(10);

            Assert.Greater(enemy.Suspicion, 0.2f);
            Assert.LessOrEqual(enemy.Suspicion, perception.hearingCap);
            Assert.AreEqual(MemorySource.Hearing, enemy.Memory.Source);
            Assert.AreEqual(noise, enemy.Memory.Position);
            Assert.Greater(enemy.Perception.HeardLoudness, 0f);
        }

        [Test]
        public void Hearing_ANoiseOutOfReachIsIgnored()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));

            HorrorAI.EmitNoise(new Vector3(0f, 0f, 30f), 10f);
            Tick(10);

            Assert.AreEqual(0f, enemy.Suspicion);
            Assert.IsFalse(enemy.Memory.HasMemory);
        }

        [Test]
        public void Hearing_WallsDampTheNoise()
        {
            var open = HeardLevel(false);
            var walled = HeardLevel(true);

            Assert.Greater(open, 0f);
            Assert.AreEqual(open * 0.45f, walled, 0.02f);
        }

        [Test]
        public void Hearing_HearingAloneNeverMakesTheEnemyChase()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out var perception, out _));
            for (var i = 0; i < 20; i++)
            {
                HorrorAI.EmitNoise(new Vector3(0f, 0f, -2f), 20f);
                Tick(6);
            }

            Assert.AreEqual(perception.hearingCap, enemy.Suspicion, 1e-4f);
            Assert.AreNotEqual(HorrorState.Chase, enemy.State);
        }

        [Test]
        public void Hearing_AnEnemyIgnoresItsOwnNoises()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));

            HorrorAI.EmitNoise(Vector3.zero, 10f, enemy.gameObject);
            Tick(10);

            Assert.AreEqual(0f, enemy.Suspicion);
        }

        [Test]
        public void TimeSlicing_EachEnemyPerceivesAtItsInterval()
        {
            var archetype = Archetype(out var perception, out _);
            perception.updateInterval = 0.1f;
            for (var i = 0; i < 10; i++)
                Enemy(new Vector3(i * 2f, 0f, 0f), Vector3.forward, archetype);
            Target(new Vector3(0f, 0f, 5f));

            var total = 0;
            for (var frame = 0; frame < 60; frame++)
            {
                Tick(1);
                total += World.Stats.Perceptions;
                Assert.LessOrEqual(World.Stats.Perceptions, 10);
            }

            Assert.That(total, Is.InRange(90, 110));
        }

        [Test]
        public void Budget_RaycastsPerFrameNeverExceedTheBudget()
        {
            var archetype = Archetype(out var perception, out _);
            perception.updateInterval = 0.02f;
            for (var i = 0; i < 60; i++)
                Enemy(new Vector3(i % 10 * 2f, 0f, i / 10 * -2f), Vector3.forward, archetype);
            Target(new Vector3(5f, 0f, 8f));
            Lamp(new Vector3(5f, 2f, 9f), 8f);
            World.RaycastBudget = 30;

            var perceived = new bool[60];
            for (var frame = 0; frame < 120; frame++)
            {
                if (frame % 5 == 0)
                    HorrorAI.EmitNoise(new Vector3(5f, 0f, 2f), 8f);
                Tick(1);
                Assert.LessOrEqual(World.Stats.Raycasts, 30);
                for (var e = 0; e < 60; e++)
                    perceived[e] |= World.Enemies[e].Perception.LastUpdate > 0f;
            }

            Assert.Greater(World.Stats.Deferred, 0);
            Assert.That(perceived, Has.All.True);
        }

        [Test]
        public void Budget_CannotBeSetBelowWhatOneEnemyNeeds()
        {
            World.RaycastBudget = 1;
            Assert.AreEqual(HorrorWorld.MinimumBudget, World.RaycastBudget);
        }

        private float SpotTime(bool lit)
        {
            DestroyCreated();
            CreateWorld();
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            Target(new Vector3(0f, 0f, 8f));
            Lamp(new Vector3(0f, 2.5f, 8.5f), 6f, lit);
            return SecondsUntil(() => enemy.Suspicion >= 1f, 30f);
        }

        private float HeardLevel(bool wall)
        {
            DestroyCreated();
            CreateWorld();
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            var blocker = wall ? Wall(new Vector3(0f, 1.5f, -3f), new Vector3(6f, 3f, 0.3f)) : null;
            HorrorAI.EmitNoise(new Vector3(0f, 0.5f, -5f), 12f);
            Tick(10);
            if (blocker != null)
                Object.DestroyImmediate(blocker);
            return enemy.Perception.HeardLoudness;
        }
    }
}
