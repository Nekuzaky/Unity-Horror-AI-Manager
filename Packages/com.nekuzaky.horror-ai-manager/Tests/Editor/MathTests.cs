using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class MathTests
    {
        [Test]
        public void InCone_SeesInsideTheAngleAndRange()
        {
            Assert.IsTrue(SightMath.InCone(Vector3.zero, Vector3.forward, new Vector3(3f, 0f, 10f), 90f, 15f, 0f));
            Assert.IsTrue(SightMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 14.9f), 90f, 15f, 0f));
        }

        [Test]
        public void InCone_MissesBehindOutsideTheAngleAndTooFar()
        {
            Assert.IsFalse(SightMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -5f), 90f, 15f, 0f));
            Assert.IsFalse(SightMath.InCone(Vector3.zero, Vector3.forward, new Vector3(10f, 0f, 5f), 90f, 15f, 0f));
            Assert.IsFalse(SightMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 15.1f), 90f, 15f, 0f));
        }

        [Test]
        public void InCone_ProximityWorksInEveryDirection()
        {
            Assert.IsTrue(SightMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -1f), 90f, 15f, 1.5f));
            Assert.IsFalse(SightMath.InCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -2f), 90f, 15f, 1.5f));
        }

        [Test]
        public void GainRate_DarknessSlowsTheGaugeByDarkGain()
        {
            var lit = SightMath.GainRate(2f, 1f, 1f, 0.12f, 2f, 18f, 3f, 0.35f, 1f);
            var dark = SightMath.GainRate(2f, 1f, 0f, 0.12f, 2f, 18f, 3f, 0.35f, 1f);
            Assert.AreEqual(2f, lit, 1e-5f);
            Assert.AreEqual(0.24f, dark, 1e-5f);
        }

        [Test]
        public void GainRate_DistanceCrouchAndPartialViewSlowTheGauge()
        {
            var close = SightMath.GainRate(2f, 1f, 1f, 0.12f, 3f, 18f, 3f, 0.35f, 1f);
            var far = SightMath.GainRate(2f, 1f, 1f, 0.12f, 18f, 18f, 3f, 0.35f, 1f);
            var crouched = SightMath.GainRate(2f, 1f, 1f, 0.12f, 3f, 18f, 3f, 0.35f, 0.5f);
            var partial = SightMath.GainRate(2f, 1f / 3f, 1f, 0.12f, 3f, 18f, 3f, 0.35f, 1f);
            Assert.AreEqual(0.7f, far, 1e-5f);
            Assert.AreEqual(close * 0.5f, crouched, 1e-5f);
            Assert.AreEqual(close / 3f, partial, 1e-5f);
            Assert.AreEqual(0f, SightMath.GainRate(2f, 0f, 1f, 0.12f, 3f, 18f, 3f, 0.35f, 1f));
        }

        [Test]
        public void Integrate_RisesWhileSeenAndDecaysOnlyAfterTheDelay()
        {
            Assert.AreEqual(0.5f, SightMath.Integrate(0.3f, 2f, 0.2f, 1.5f, 0f, 0.1f), 1e-5f);
            Assert.AreEqual(1f, SightMath.Integrate(0.9f, 2f, 0.2f, 1.5f, 0f, 1f), 1e-5f);
            Assert.AreEqual(0.5f, SightMath.Integrate(0.5f, 0f, 0.2f, 1.5f, 1f, 0.1f), 1e-5f);
            Assert.AreEqual(0.48f, SightMath.Integrate(0.5f, 0f, 0.2f, 1.5f, 1.6f, 0.1f), 1e-5f);
            Assert.AreEqual(0.49f, SightMath.Integrate(0.5f, 0f, 0.2f, 1.5f, 1.55f, 0.1f), 1e-5f);
        }

        [Test]
        public void LightFalloff_IsFullAtTheLampAndZeroAtItsRange()
        {
            Assert.AreEqual(1f, LightMath.Falloff(0f, 8f), 1e-5f);
            Assert.AreEqual(0f, LightMath.Falloff(8f, 8f), 1e-5f);
            Assert.AreEqual(0.5625f, LightMath.Falloff(4f, 8f), 1e-5f);
            Assert.Greater(LightMath.Falloff(2f, 8f), LightMath.Falloff(6f, 8f));
        }

        [Test]
        public void SpotLight_LightsInsideTheBeamOnly()
        {
            var inside = LightMath.Contribution(Vector3.zero, Vector3.forward, true, 60f, 10f, 1f, new Vector3(0f, 0f, 2f));
            var edge = LightMath.Contribution(Vector3.zero, Vector3.forward, true, 60f, 10f, 1f, new Vector3(1.1f, 0f, 2f));
            var outside = LightMath.Contribution(Vector3.zero, Vector3.forward, true, 60f, 10f, 1f, new Vector3(3f, 0f, 2f));
            var behind = LightMath.Contribution(Vector3.zero, Vector3.forward, true, 60f, 10f, 1f, new Vector3(0f, 0f, -2f));
            Assert.AreEqual(LightMath.Falloff(2f, 10f), inside, 1e-5f);
            Assert.Greater(edge, 0f);
            Assert.Less(edge, inside);
            Assert.AreEqual(0f, outside);
            Assert.AreEqual(0f, behind);
        }

        [Test]
        public void Exposure_AddsAmbientAndClampsToOne()
        {
            Assert.AreEqual(0.04f, LightMath.Exposure(0.04f, 0f), 1e-5f);
            Assert.AreEqual(1f, LightMath.Exposure(0.04f, 3f), 1e-5f);
        }

        [Test]
        public void Hearing_FadesLinearlyWithDistanceAndStopsAtTheReach()
        {
            Assert.AreEqual(1f, HearingMath.Perceived(10f, 0f, 1f, 0, 0.5f), 1e-5f);
            Assert.AreEqual(0.5f, HearingMath.Perceived(10f, 5f, 1f, 0, 0.5f), 1e-5f);
            Assert.AreEqual(0f, HearingMath.Perceived(10f, 10f, 1f, 0, 0.5f));
            Assert.AreEqual(0.75f, HearingMath.Perceived(10f, 5f, 2f, 0, 0.5f), 1e-5f);
        }

        [Test]
        public void Hearing_EachWallDampsTheNoise()
        {
            var open = HearingMath.Perceived(10f, 2f, 1f, 0, 0.45f);
            Assert.AreEqual(open * 0.45f, HearingMath.Perceived(10f, 2f, 1f, 1, 0.45f), 1e-5f);
            Assert.AreEqual(open * 0.45f * 0.45f, HearingMath.Perceived(10f, 2f, 1f, 2, 0.45f), 1e-5f);
        }

        [Test]
        public void FromSound_DerivesLoudnessFromVolumeAndMaxDistance()
        {
            Assert.AreEqual(10f, HearingMath.FromSound(1f, 20f, 0.5f, 0f), 1e-5f);
            Assert.AreEqual(5f, HearingMath.FromSound(0.5f, 20f, 0.5f, 0f), 1e-5f);
            Assert.AreEqual(0f, HearingMath.FromSound(1f, 0f, 0.5f, 0f));
        }

        [Test]
        public void Memory_LosesCertaintyOverTimeThenForgets()
        {
            var memory = new HorrorMemory();
            memory.Remember(new Vector3(1f, 0f, 2f), 3f, 0.5f, MemorySource.Hearing);
            memory.Decay(0.1f, 2f);
            Assert.AreEqual(0.3f, memory.Certainty, 1e-5f);
            Assert.IsTrue(memory.HasMemory);
            memory.Decay(0.1f, 10f);
            Assert.IsFalse(memory.HasMemory);
            Assert.AreEqual(MemorySource.None, memory.Source);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), memory.Position);
        }

        [Test]
        public void Memory_KeepsTheHighestCertaintyAndTheNewestPosition()
        {
            var memory = new HorrorMemory();
            memory.Remember(Vector3.zero, 1f, 0.8f, MemorySource.Sight);
            memory.Remember(Vector3.one, 2f, 0.2f, MemorySource.Hearing);
            Assert.AreEqual(0.8f, memory.Certainty, 1e-5f);
            Assert.AreEqual(Vector3.one, memory.Position);
            Assert.AreEqual(2f, memory.Time);
        }

        [Test]
        public void PatrolAdvance_LoopsPingPongsOrStops()
        {
            var direction = 1;
            Assert.AreEqual(0, PatrolRoute.Advance(2, 3, PatrolMode.Loop, ref direction));
            Assert.AreEqual(1, PatrolRoute.Advance(2, 3, PatrolMode.PingPong, ref direction));
            Assert.AreEqual(-1, direction);
            Assert.AreEqual(0, PatrolRoute.Advance(1, 3, PatrolMode.PingPong, ref direction));
            Assert.AreEqual(1, PatrolRoute.Advance(0, 3, PatrolMode.PingPong, ref direction));
            Assert.AreEqual(2, PatrolRoute.Advance(2, 3, PatrolMode.Once, ref direction));
        }

        [Test]
        public void Random_IsDeterministicForASeed()
        {
            var a = new HorrorRandom(42u);
            var b = new HorrorRandom(42u);
            for (var i = 0; i < 100; i++)
            {
                var value = a.Next01();
                Assert.AreEqual(value, b.Next01());
                Assert.That(value, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void Transition_ExplainsEveryReasonInPlainWords()
        {
            foreach (TransitionReason reason in System.Enum.GetValues(typeof(TransitionReason)))
                Assert.IsFalse(string.IsNullOrEmpty(HorrorTransition.Explain(reason)));
        }
    }
}
