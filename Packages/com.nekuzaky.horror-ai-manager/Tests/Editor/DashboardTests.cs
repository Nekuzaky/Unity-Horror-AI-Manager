using System.Collections.Generic;
using Nekuzaky.HorrorAI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class DashboardTests : Fixture
    {
        private static string _text;
        private DashboardSampler _sampler;

        [SetUp]
        public void CreateSampler()
        {
            _sampler = new DashboardSampler();
            _sampler.Attach();
        }

        [TearDown]
        public void DetachSampler() => _sampler.Detach();

        [Test]
        public void Sampler_AllocatesNothingOnceWarmedUp()
        {
            Crowd(8);
            Tick(DashboardSampler.FrameCapacity + 30);

            Assert.That(() => World.Tick(Frame), Is.Not.AllocatingGCMemory());
            Assert.That(() => _sampler.Ticked(World), Is.Not.AllocatingGCMemory());
            Assert.That(() => _sampler.Summarize(), Is.Not.AllocatingGCMemory());
            Assert.AreEqual(DashboardSampler.FrameCapacity, _sampler.FrameCost.Count);
            Assert.Greater(_sampler.SuspicionHistory.Count, 0);
        }

        [Test]
        public void OpenDashboard_AddsNoGarbageToTheUpdate()
        {
            Crowd(4);
            Tick(10);
            var worst = 0L;
            var transitions = 0;
            for (var i = 0; i < 600; i++)
            {
                if (i % 90 == 0)
                    HorrorAI.EmitNoise(new Vector3(0f, 0f, (i % 180) - 4f), 10f);
                World.Tick(Frame);
                worst = System.Math.Max(worst, World.Stats.AllocatedBytes);
            }
            foreach (var enemy in World.Enemies)
                transitions += _sampler.TrackOf(enemy).TransitionCount;

            Assert.Greater(transitions, 4);
            Assert.AreEqual(0L, worst);
        }

        [Test]
        public void Sampler_RecordsEveryFrameTransitionsAndHearings()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            HorrorAI.EmitNoise(new Vector3(0f, 0f, -3f), 10f);

            Tick(30);

            var track = _sampler.TrackOf(enemy);
            Assert.AreEqual(30, _sampler.Frames);
            Assert.IsNotNull(track);
            Assert.Greater(track.TransitionCount, 0);
            Assert.AreEqual(HorrorState.Alert, track.Transition(0).To);
            Assert.AreEqual(TransitionReason.HeardNoise, track.Transition(0).Reason);
            Assert.AreEqual(1, _sampler.HearingCount);
            Assert.IsTrue(_sampler.Hearing(0).Heard);
            Assert.AreSame(enemy, _sampler.Hearing(0).Enemy);
        }

        [Test]
        public void ClosedDashboard_DoesNoWork()
        {
            Crowd(2);
            _sampler.Detach();

            Tick(20);

            Assert.IsNull(HorrorProbe.Listener);
            Assert.AreEqual(0, _sampler.Frames);
            Assert.That(() => World.Tick(Frame), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Sampler_ForgetsAnOldWorld()
        {
            Crowd(1);
            Tick(10);

            var next = HorrorAI.ResetWorld();
            next.Tick(Frame);

            Assert.AreEqual(1, _sampler.FrameCost.Count);
        }

        [Test]
        public void Percentile95_KeepsTheSlowFrames()
        {
            var ring = new FloatRing(100);
            for (var i = 1; i <= 100; i++)
                ring.Push(i);

            Assert.AreEqual(96f, ring.Percentile95(new float[100]));
            Assert.AreEqual(50.5f, ring.Average(), 1e-4f);
            Assert.AreEqual(100f, ring.Max());
        }

        [Test]
        public void Text_ReusesTheSameStrings()
        {
            Assert.AreSame(Text.Percent(0.42f), Text.Percent(0.421f));
            Assert.AreSame(Text.Milliseconds(0.0441f), Text.Milliseconds(0.0439f));
            Assert.AreEqual("0.044 ms", Text.Milliseconds(0.044f));
            Assert.AreEqual("2.5 s ago", Text.Ago(2.5f));
            Assert.AreEqual(Text.NotMeasured, Text.Bytes(-1L));
            Assert.AreEqual("0 B", Text.Bytes(0L));
            Assert.That(() => { _text = Text.Percent(0.42f); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void EnemyRow_RebuildsNothingWhenNothingChanged()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            Target(new Vector3(0f, 0f, 4f));
            Tick(60);
            var row = new EnemyRow();
            row.Bind(enemy);
            row.Update(null);
            var name = row.NameLabel.text;
            var chip = row.Chip.text;

            Assert.That(() => row.Update(null), Is.Not.AllocatingGCMemory());
            Assert.AreSame(name, row.NameLabel.text);
            Assert.AreSame(chip, row.Chip.text);
            Assert.AreEqual(Ui.StateName(enemy.State), row.Chip.text);
            Assert.AreEqual(enemy.LastTransition.Explanation, row.Reason.text);
        }

        [Test]
        public void Overview_RefreshAllocatesNothingInSteadyState()
        {
            Crowd(6);
            Tick(DashboardSampler.FrameCapacity);
            var view = new OverviewView();
            view.Refresh(_sampler, true);
            view.Refresh(_sampler, true);

            Assert.That(() => view.Refresh(_sampler, true), Is.Not.AllocatingGCMemory());
            Assert.AreEqual(6, CountAll(view));
            Assert.AreNotEqual(Text.None, view.Cost.Value.text);
            Assert.AreEqual("0 B", view.Garbage.Value.text);
        }

        [Test]
        public void EnemyDetail_ShowsTheHistoryWithoutAllocating()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            HorrorAI.EmitNoise(new Vector3(0f, 0f, -3f), 10f);
            Tick(120);
            var detail = new EnemyDetail();
            detail.Show(enemy, _sampler);
            detail.Show(enemy, _sampler);

            Assert.That(() => detail.Show(enemy, _sampler), Is.Not.AllocatingGCMemory());
            Assert.AreEqual(_sampler.TrackOf(enemy).TransitionCount, detail.VisibleTransitions);
            Assert.Greater(detail.VisibleTransitions, 0);
        }

        [Test]
        public void Noises_SayWhoHeardThemThroughWalls()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Archetype(out _, out _));
            enemy.name = "Ghoul";
            Wall(new Vector3(0f, 1f, -1.5f), new Vector3(4f, 3f, 0.2f));
            HorrorAI.EmitNoise(new Vector3(0f, 0.5f, -3f), 12f);
            Tick(30);
            var view = new NoisesView();
            view.Refresh(_sampler);

            StringAssert.Contains("Ghoul", view.HeardBy(0));
            StringAssert.Contains("1 wall", view.HeardBy(0));
            Assert.AreSame(view.HeardBy(0), view.HeardBy(0));
        }

        [Test]
        public void Noises_NameTheHearersWithFiftyEnemies()
        {
            var archetype = Archetype(out _, out _);
            for (var i = 0; i < 50; i++)
            {
                var angle = i * Mathf.PI * 2f / 50f;
                Enemy(new Vector3(Mathf.Cos(angle) * 4f, 0f, Mathf.Sin(angle) * 4f), Vector3.forward, archetype);
            }
            for (var i = 0; i < 8; i++)
            {
                HorrorAI.EmitNoise(new Vector3(0f, 0.5f, 0f), 20f);
                Tick(15);
            }
            Tick(120);
            var view = new NoisesView();
            view.Refresh(_sampler);

            Assert.Greater(50 * 8, DashboardSampler.HearingCapacity);
            Assert.AreEqual(50, _sampler.HearersOf(0).HeardCount);
            StringAssert.Contains("Enemy", view.HeardBy(0));
            StringAssert.Contains("and 44 more", view.HeardBy(0));
            StringAssert.DoesNotContain("nobody", view.HeardBy(0));
            Assert.AreEqual(50, _sampler.HearersOf(7).HeardCount);
            var enemy = World.Enemies[0];
            Assert.That(() => _sampler.Heard(enemy, 7, 0.9f, 1, true), Is.Not.AllocatingGCMemory());
            Assert.That(() => _sampler.Heard(enemy, 8, 0.1f, 0, false), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Noises_SayNoNoiseYetInPlainWords()
        {
            var view = new NoisesView();
            view.Refresh(_sampler);

            Assert.AreEqual(0, view.Items.Count);
            Assert.AreEqual("No noise yet.", view.Empty.text);
            Assert.AreEqual(UnityEngine.UIElements.DisplayStyle.Flex, view.Empty.style.display.value);
        }

        [Test]
        public void Overview_ForgetsPlayModeWhenItStops()
        {
            Crowd(3);
            Tick(60);
            var view = new OverviewView();
            view.Refresh(_sampler, true);

            Assert.IsNotNull(view.CostGraph.Source);
            Assert.AreEqual("Enemies running", view.Scene.Label.text);
            Assert.Greater(CountAll(view), 0);

            view.Refresh(_sampler, false);

            Assert.IsNull(view.CostGraph.Source);
            Assert.AreEqual(Text.None, view.CostValue.text);
            Assert.AreEqual(Text.None, view.Cost.Value.text);
            Assert.AreEqual(Text.None, view.Garbage.Value.text);
            Assert.AreEqual("Enemies in the scene", view.Scene.Label.text);
            Assert.AreEqual(0, CountAll(view));
        }

        [Test]
        public void EnemyRow_InEditModeShowsNoTarget()
        {
            var archetype = Archetype(out _, out _);
            archetype.name = "Stalker";
            var enemy = Enemy(Vector3.zero, Vector3.forward, archetype, false);
            var row = new EnemyRow();
            row.Bind(enemy);
            row.Update("ready to run");

            Assert.AreEqual(EnemyRow.NotRunning, row.Chip.text);
            Assert.AreEqual(Text.None, row.TargetLabel.text);
            Assert.AreEqual("ready to run", row.Reason.text);
        }

        [Test]
        public void Enemies_DropDestroyedEnemiesAndStackWhenNarrow()
        {
            var archetype = Archetype(out _, out _);
            var first = Enemy(Vector3.zero, Vector3.forward, archetype, false);
            Enemy(new Vector3(3f, 0f, 0f), Vector3.forward, archetype, false);
            var view = new EnemiesView();
            view.SceneChanged();
            view.Refresh(_sampler);
            var before = view.Items.Count;

            Object.DestroyImmediate(first.gameObject);
            view.Refresh(_sampler);

            Assert.GreaterOrEqual(before, 2);
            Assert.AreEqual(before - 1, view.Items.Count);
            foreach (var enemy in view.Items)
                Assert.IsTrue(enemy != null);
            view.Resize(500f);
            Assert.IsTrue(view.Narrow);
            view.Resize(900f);
            Assert.IsFalse(view.Narrow);
        }

        [Test]
        public void AddTarget_IgnoresTheSelectionAndCreatesAPlayer()
        {
            var navMesh = Track(new GameObject("NavMesh"));
            navMesh.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            Selection.activeGameObject = navMesh;
            HorrorTarget target = null;
            try
            {
                target = SceneFixes.AddTarget();

                Assert.IsNull(navMesh.GetComponent<HorrorTarget>());
                Assert.AreNotSame(navMesh, target.gameObject);
                Assert.IsFalse(SceneFixes.IsMisplaced(target));
                if (GameObject.FindWithTag("Player") == null && Camera.main == null)
                    Assert.AreEqual("Player", target.gameObject.name);
            }
            finally
            {
                Selection.activeGameObject = null;
                if (target != null && target.gameObject.name == "Player")
                    Object.DestroyImmediate(target.gameObject);
                else if (target != null)
                    Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Doctor_WarnsWhenTheTargetIsOnTheNavMesh()
        {
            var navMesh = Track(new GameObject("NavMesh"));
            navMesh.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            navMesh.AddComponent<HorrorTarget>();
            var checks = new List<DoctorCheck>();

            DoctorChecks.Run(checks);

            Assert.IsTrue(Has(checks, "not on the player", CheckLevel.Warning));
            Assert.IsFalse(Has(checks, "Horror Target", CheckLevel.Ok));
        }

        [Test]
        public void Doctor_ExplainsWhatIsMissingInTheScene()
        {
            Enemy(new Vector3(500f, 0f, 500f), Vector3.forward, null, false);
            var checks = new List<DoctorCheck>();

            DoctorChecks.Run(checks);

            Assert.IsTrue(Has(checks, "no archetype", CheckLevel.Warning));
            Assert.IsTrue(Has(checks, "No Horror Target", CheckLevel.Error));
            Assert.IsTrue(Has(checks, "no NavMesh Agent", CheckLevel.Warning));
            Assert.IsTrue(Has(checks, "no patrol route", CheckLevel.Info));
            foreach (var check in checks)
            {
                if (check.Level == CheckLevel.Error || check.Level == CheckLevel.Warning)
                    Assert.IsNotNull(check.Title);
            }
        }

        [Test]
        public void Archetypes_CreateACopyOfThePatroller()
        {
            const string folder = "Assets/HorrorAITestArchetypes";
            AssetDatabase.CreateFolder("Assets", "HorrorAITestArchetypes");
            try
            {
                var archetype = ArchetypesView.CreateFromPreset(folder + "/Stalker.asset");

                Assert.IsNotNull(archetype.perception);
                Assert.IsNotNull(archetype.behavior);
                Assert.AreNotSame(SceneFixes.Patroller.perception, archetype.perception);
                Assert.AreEqual(SceneFixes.Patroller.perception.sightGain, archetype.perception.sightGain);
                Assert.IsTrue(AssetDatabase.LoadAssetAtPath<PerceptionProfile>(folder + "/Stalker Perception.asset") != null);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void Icons_AreAssignedToEveryComponentAndAsset()
        {
            foreach (var name in Glyphs.Tiles)
                Assert.IsNotNull(Glyphs.Tile(name), name);
            foreach (var name in Glyphs.All)
                Assert.IsNotNull(Glyphs.Get(name), name);
            Assert.IsNotNull(Glyphs.StyleSheet);
            var scripts = new[] { typeof(HorrorEnemy), typeof(HorrorTarget), typeof(HorrorLightSource), typeof(NoiseEmitter), typeof(PatrolRoute),
                typeof(HorrorArchetype), typeof(PerceptionProfile), typeof(BehaviorProfile) };
            var partial = AssetDatabase.LoadAssetAtPath<MonoScript>(SceneFixes.PackagePath + "/Runtime/src/Components/HorrorEnemy.Perception.cs");
            Assert.IsNotNull(partial);
            Assert.AreSame(Glyphs.Tile(nameof(HorrorEnemy)), EditorGUIUtility.GetIconForObject(partial));
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SceneFixes.PackagePath + "/Editor/Icons" }))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsFalse(importer.mipmapEnabled, importer.assetPath);
            }
            foreach (var type in scripts)
            {
                var script = Script(type);
                Assert.IsNotNull(script, type.Name);
                var icon = EditorGUIUtility.GetIconForObject(script);
                Assert.IsNotNull(icon, type.Name + " has no icon");
                Assert.AreSame(Glyphs.Tile(type.Name), icon, type.Name);
            }
        }

        [Test]
        public void Window_OpensEveryTabAndStopsListeningWhenClosed()
        {
            var window = DashboardWindow.Open();
            try
            {
                Assert.IsTrue(window.Sampler.IsAttached);
                for (var i = 0; i < window.Views.Count; i++)
                {
                    window.ShowTab(i);
                    window.RefreshNow();
                }
                Assert.AreEqual(5, window.Views.Count);
                Assert.Greater(window.SetupPage.Checks.Count, 0);
            }
            finally
            {
                window.Close();
            }
            Assert.IsNull(HorrorProbe.Listener);
        }

        private void Crowd(int count)
        {
            var archetype = Archetype(out _, out _);
            Target(new Vector3(0f, 0f, 6f));
            for (var i = 0; i < count; i++)
                Enemy(new Vector3(i * 3f - count, 0f, 0f), Vector3.forward, archetype);
            HorrorAI.EmitNoise(new Vector3(0f, 0f, 2f), 8f);
        }

        private static int CountAll(OverviewView view)
        {
            var total = 0;
            for (var i = 0; i < 6; i++)
                total += view.CountOf((HorrorState)i);
            return total;
        }

        private static bool Has(List<DoctorCheck> checks, string fragment, CheckLevel level)
        {
            foreach (var check in checks)
            {
                if (check.Level == level && check.Title.Contains(fragment))
                    return true;
            }
            return false;
        }

        private static MonoScript Script(System.Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript", new[] { "Packages/com.nekuzaky.horror-ai-manager" }))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == type)
                    return script;
            }
            return null;
        }
    }
}
