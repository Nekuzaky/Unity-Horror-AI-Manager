using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class SessionTests : Fixture
    {
        [TearDown]
        public void EndSession() => HorrorAI.ResetSession();

        [Test]
        public void TwoPlaySessions_LeaveASingleRunner()
        {
            HorrorAI.ResetSession();
            HorrorRunner.Ensure();
            var first = HorrorRunner.Current;

            HorrorAI.ResetSession();
            HorrorRunner.Ensure();

            Assert.IsTrue(first == null, "The runner of the first session survived the second one.");
            Assert.IsTrue(HorrorRunner.Current != null);
            Assert.AreEqual(1, LiveRunners());
        }

        [Test]
        public void AStaleRunner_DestroysItselfInsteadOfTicking()
        {
            HorrorAI.ResetSession();
            HorrorRunner.Ensure();
            var stale = new GameObject("Stale Horror AI").AddComponent<HorrorRunner>();
            var frame = HorrorAI.Stats.Frame;

            stale.Run(Frame);

            Assert.IsTrue(stale == null, "A runner that is not the current one kept running.");
            Assert.AreEqual(frame, HorrorAI.Stats.Frame);
            HorrorRunner.Current.Run(Frame);
            Assert.AreEqual(frame + 1, HorrorAI.Stats.Frame);
        }

        [Test]
        public void ANewSession_DropsTheStaticEventHandlers()
        {
            var calls = 0;
            HorrorAI.StateChanged += (_, _) => calls++;
            HorrorAI.NoiseEmitted += _ => calls++;
            HorrorAI.EnemyAdded += _ => calls++;
            HorrorAI.EnemyRemoved += _ => calls++;

            HorrorAI.ResetSession();
            HorrorAI.RaiseStateChanged(null, default);
            HorrorAI.RaiseEnemyAdded(null);
            HorrorAI.RaiseEnemyRemoved(null);
            HorrorAI.EmitNoise(Vector3.zero, 5f);

            Assert.AreEqual(0, calls);
        }

        private static int LiveRunners()
        {
            var count = 0;
            foreach (var runner in Resources.FindObjectsOfTypeAll<HorrorRunner>())
            {
                if (runner != null)
                    count++;
            }
            return count;
        }
    }
}
