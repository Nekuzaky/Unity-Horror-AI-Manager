using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Debug = UnityEngine.Debug;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class PerformanceTests : Fixture
    {
        private static object _keep;

        [Test]
        public void AllocationCheck_SeesSmallAllocations()
        {
            Assert.That(() => { _keep = new int[4]; }, Is.AllocatingGCMemory());
        }

        [Test]
        public void PerceptionAndUpdate_AllocateNothingOnceWarmedUp()
        {
            BuildScene(30);

            Cycle(120);

            Assert.That(() => Cycle(600), Is.Not.AllocatingGCMemory());
            Assert.AreEqual(0, World.Stats.AllocatedBytes);
        }

        [Test]
        public void StateTransitionsAndEvents_AllocateNothingOnceWarmedUp()
        {
            var target = BuildScene(10);
            foreach (var enemy in World.Enemies)
            {
                enemy.onSpotted.AddListener(Nothing);
                enemy.onLostTrack.AddListener(Nothing);
            }

            Flicker(target, 900);

            Assert.That(() => Flicker(target, 900), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void FiftyEnemies_CostLittleAndRespectTheBudget()
        {
            BuildScene(50);
            Cycle(120);
            var watch = Stopwatch.StartNew();
            var maxRays = 0;
            for (var i = 0; i < 600; i++)
            {
                World.Tick(Frame);
                maxRays = Mathf.Max(maxRays, World.Stats.Raycasts);
            }
            watch.Stop();

            var milliseconds = watch.Elapsed.TotalMilliseconds / 600d;
            Debug.Log($"[HorrorAI] 50 enemies in Edit Mode: {milliseconds:0.000} ms per tick, at most {maxRays} rays per frame");
            Assert.LessOrEqual(maxRays, World.RaycastBudget);
            Assert.Less(milliseconds, 1d);
        }

        private HorrorTarget BuildScene(int enemies)
        {
            var archetype = Archetype(out _, out _);
            for (var i = 0; i < enemies; i++)
            {
                var angle = i * Mathf.PI * 2f / enemies;
                var position = new Vector3(Mathf.Cos(angle) * 9f, 0f, Mathf.Sin(angle) * 9f);
                Enemy(position, -position, archetype);
            }
            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI * 0.5f + 0.4f;
                Wall(new Vector3(Mathf.Cos(angle) * 5f, 1.5f, Mathf.Sin(angle) * 5f), new Vector3(2f, 3f, 0.3f));
                Lamp(new Vector3(Mathf.Cos(angle) * 3f, 2.5f, Mathf.Sin(angle) * 3f), 6f);
            }
            return Target(Vector3.zero);
        }

        private void Cycle(int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                if (i % 10 == 0)
                    HorrorAI.EmitNoise(new Vector3(i % 7, 0f, 2f), 8f);
                World.Tick(Frame);
            }
        }

        private void Flicker(HorrorTarget target, int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                var visible = i % 300 < 150;
                target.visibilityMultiplier = visible ? 1f : 0f;
                target.transform.position = visible ? Vector3.zero : new Vector3(0f, -50f, 0f);
                Physics.SyncTransforms();
                World.Tick(Frame);
            }
        }

        private static void Nothing()
        {
        }
    }
}
