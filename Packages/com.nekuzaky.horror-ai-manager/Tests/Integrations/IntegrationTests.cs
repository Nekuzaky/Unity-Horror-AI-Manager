using System.Collections.Generic;
using System.Reflection;
using Nekuzaky.HorrorAI.Integrations;
using Nekuzaky.Resonance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class IntegrationTests
    {
        #region Private and Protected

        private readonly List<Object> _created = new();
        private readonly List<HorrorNoise> _noises = new();
        private static string _label;

        [SetUp]
        public void Listen()
        {
            HorrorAI.ResetWorld();
            _noises.Clear();
            HorrorAI.NoiseEmitted += OnNoise;
        }

        [TearDown]
        public void Clean()
        {
            HorrorAI.NoiseEmitted -= OnNoise;
            foreach (var created in _created)
            {
                if (created != null)
                    Object.DestroyImmediate(created);
            }
            _created.Clear();
            HorrorAI.ResetWorld();
        }

        private void OnNoise(HorrorNoise noise) => _noises.Add(noise);

        private T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        private ResonanceNoiseBridge Bridge() => Track(new GameObject("Bridge")).AddComponent<ResonanceNoiseBridge>();

        private SoundEvent Sound(string name)
        {
            var sound = Track(ScriptableObject.CreateInstance<SoundEvent>());
            sound.name = name;
            return sound;
        }

        private static SoundStartInfo Start(SoundEvent sound, Vector3 position, float volume, float maxDistance, GameObject emitter = null) =>
            new(new SoundHandle(1), sound, emitter, position, volume, maxDistance);

        #endregion


        #region Resonance

        [Test]
        public void Bridge_DerivesLoudnessFromVolumeAndMaxDistanceByDefault()
        {
            var bridge = Bridge();

            bridge.OnSoundStarted(Start(Sound("Footstep"), new Vector3(1f, 0f, 2f), 0.8f, 20f));

            Assert.AreEqual(1, _noises.Count);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), _noises[0].Position);
            Assert.AreEqual(0.8f * 20f * 0.5f, _noises[0].Loudness, 1e-4f);
            Assert.AreEqual(1, bridge.NoisesSent);
        }

        [Test]
        public void Bridge_IgnoresTwoDimensionalAndVeryQuietSounds()
        {
            var bridge = Bridge();

            bridge.OnSoundStarted(Start(Sound("Music"), Vector3.zero, 1f, 0f));
            bridge.OnSoundStarted(Start(Sound("Whisper"), Vector3.zero, 0.01f, 20f));

            Assert.IsEmpty(_noises);
        }

        [Test]
        public void Map_OverridesLoudnessAndSilencesListedSounds()
        {
            var bridge = Bridge();
            var map = Track(ScriptableObject.CreateInstance<ResonanceNoiseMap>());
            var gunshot = Sound("Gunshot");
            var voice = Sound("Enemy Voice");
            map.entries.Add(new ResonanceNoiseMap.Entry { sound = gunshot, loudness = 40f });
            map.entries.Add(new ResonanceNoiseMap.Entry { sound = voice, silent = true });
            map.useDefault = false;
            map.Rebuild();
            bridge.map = map;

            bridge.OnSoundStarted(Start(gunshot, Vector3.zero, 0.1f, 5f));
            bridge.OnSoundStarted(Start(voice, Vector3.zero, 1f, 30f));
            bridge.OnSoundStarted(Start(Sound("Door"), Vector3.zero, 1f, 30f));

            Assert.AreEqual(1, _noises.Count);
            Assert.AreEqual(40f, _noises[0].Loudness, 1e-4f);
        }

        [Test]
        public void Bridge_KeepsTheEmitterSoEnemiesIgnoreTheirOwnSounds()
        {
            var enemyObject = Track(new GameObject("Enemy"));

            Bridge().OnSoundStarted(Start(Sound("Growl"), Vector3.zero, 1f, 20f, enemyObject));

            Assert.AreSame(enemyObject, _noises[0].Source);
        }

        #endregion


        #region Hindsight

        [Test]
        public void Hindsight_BookmarkLabelNamesTheEnemyTheStatesAndTheReason()
        {
            var enemy = Track(new GameObject("Ghoul")).AddComponent<HorrorEnemy>();
            var label = HorrorHindsight.Label(enemy, new HorrorTransition(HorrorState.Search, HorrorState.Patrol, TransitionReason.GaveUp, 3f));

            Assert.AreEqual("Ghoul: Search > Patrol (gave up the search)", label);
        }

        [Test]
        public void Hindsight_LabelsAreBuiltOnceWhenTheEnemyIsAddedThenReused()
        {
            var enemy = Track(new GameObject("Ghoul")).AddComponent<HorrorEnemy>();
            HorrorHindsight.OnEnemyAdded(enemy);
            var spotted = new HorrorTransition(HorrorState.Patrol, HorrorState.Chase, TransitionReason.Spotted, 1f);
            var first = HorrorHindsight.Label(enemy, spotted);

            Assert.That(() => { _label = HorrorHindsight.Label(enemy, spotted); }, Is.Not.AllocatingGCMemory());
            Assert.AreSame(first, HorrorHindsight.Label(enemy, new HorrorTransition(HorrorState.Patrol, HorrorState.Chase, TransitionReason.Spotted, 9f)));
            HorrorHindsight.OnEnemyRemoved(enemy);
        }

        [Test]
        public void Hindsight_InstallsAfterThePlayModeResetSoItIsNeverWiped()
        {
            var install = typeof(HorrorHindsight).GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic);
            var attribute = (RuntimeInitializeOnLoadMethodAttribute)install.GetCustomAttribute(typeof(RuntimeInitializeOnLoadMethodAttribute));
            var bookmarked = 0;
            void Count(HorrorEnemy enemy, HorrorTransition transition) => bookmarked++;

            HorrorAI.ResetSession();
            HorrorHindsight.Install();
            var installed = typeof(HorrorAI).GetField("StateChanged", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            HorrorAI.StateChanged += Count;
            HorrorAI.RaiseStateChanged(null, default);
            HorrorAI.ResetSession();
            HorrorAI.NoiseEmitted += OnNoise;

            Assert.AreEqual(RuntimeInitializeLoadType.AfterAssembliesLoaded, attribute.loadType);
            Assert.AreEqual(1, bookmarked);
            Assert.IsNotNull(installed);
        }

        [Test]
        public void Hindsight_ProbeCopiesTheEnemyValues()
        {
            var go = Track(new GameObject("Ghoul"));
            var enemy = go.AddComponent<HorrorEnemy>();
            HorrorAI.World.Add(enemy);
            enemy.Investigate(new Vector3(0f, 0f, 5f));
            var probe = go.AddComponent<HorrorHindsightProbe>();

            probe.Sample();

            Assert.AreEqual(HorrorState.Investigate, probe.state);
            Assert.AreEqual(enemy.Suspicion, probe.suspicion);
            Assert.AreEqual(new Vector3(0f, 0f, 5f), probe.lastKnownPosition);
            Assert.AreEqual(1f, probe.certainty, 1e-4f);
        }

        #endregion
    }
}
