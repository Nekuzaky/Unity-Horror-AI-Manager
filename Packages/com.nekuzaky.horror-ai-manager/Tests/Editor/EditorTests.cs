using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Nekuzaky.HorrorAI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Nekuzaky.HorrorAI.Tests
{
    public sealed class EditorTests : Fixture
    {
        [Test]
        public void PatrollerPreset_ShipsWithBothProfiles()
        {
            var patroller = SceneFixes.Patroller;

            Assert.IsNotNull(patroller, "The Patroller preset is missing from " + SceneFixes.PatrollerPath);
            Assert.IsNotNull(patroller.perception);
            Assert.IsNotNull(patroller.behavior);
            Assert.IsNotEmpty(patroller.description);
            Assert.Less(patroller.behavior.calmThreshold, patroller.behavior.alertThreshold);
        }

        [Test]
        public void Archetype_FallsBackToDefaultsWhenAProfileIsMissing()
        {
            var archetype = Asset<HorrorArchetype>();

            Assert.IsNotNull(archetype.Perception);
            Assert.IsNotNull(archetype.Behavior);
            Assert.IsNotNull(HorrorArchetype.Default.Perception);
        }

        [Test]
        public void EnemyInspector_ExplainsWhatIsMissing()
        {
            var enemy = Enemy(new Vector3(500f, 0f, 500f), Vector3.forward, null, false);
            var warnings = new Warnings();

            EnemyInspector.Collect(enemy, warnings);

            Assert.IsTrue(warnings.Contains("No archetype"));
            Assert.IsTrue(warnings.Contains("No NavMesh Agent"));
            Assert.IsTrue(warnings.Contains("no Horror Target"));
            Assert.IsTrue(warnings.Contains("No patrol route"));
        }

        [Test]
        public void EnemyInspector_ReportsAMissingNavMeshOnceAnAgentIsThere()
        {
            var enemy = Enemy(new Vector3(500f, 0f, 500f), Vector3.forward, null, false);
            enemy.gameObject.AddComponent<NavMeshAgent>();
            var warnings = new Warnings();

            EnemyInspector.Collect(enemy, warnings);

            Assert.IsTrue(warnings.Contains("no NavMesh under this enemy"));
            Assert.IsFalse(warnings.Contains("No NavMesh Agent"));
        }

        [Test]
        public void CreateRoute_GivesTheEnemyATwoPointRoute()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, null, false);

            var route = Track(SceneFixes.CreateRoute(enemy));
            Track(route.gameObject);

            Assert.AreSame(route, enemy.patrolRoute);
            Assert.AreEqual(2, route.Count);
            Assert.AreEqual(new Vector3(0f, 0f, 6f), route.GetPosition(1));
        }

        [Test]
        public void Window_DescribesAnEnemyInPlainWords()
        {
            var enemy = Enemy(Vector3.zero, Vector3.forward, Asset<HorrorArchetype>());
            HorrorAI.EmitNoise(new Vector3(0f, 0f, -3f), 10f);
            Tick(10);

            StringAssert.Contains("heard a noise", HorrorAIWindow.Describe(enemy));
            StringAssert.Contains("Hearing", HorrorAIWindow.Memory(enemy));
            StringAssert.Contains("heard a noise", enemy.LastTransition.Explanation);
        }

        [Test]
        public void ProfileText_SummarisesTheArchetype()
        {
            var text = ProfileText.Summary(Asset<PerceptionProfile>(), Asset<BehaviorProfile>());

            StringAssert.Contains("spotted in 0.5 s when lit", text);
            StringAssert.Contains("searches for 18 s", text);
        }

        [Test]
        public void ProfileText_CountsTheAmbientLightInTheDark()
        {
            World.AmbientLight = 0.04f;

            var text = ProfileText.Sight(Asset<PerceptionProfile>());

            StringAssert.Contains("3.2 s in the dark close by", text);
            StringAssert.Contains("9.2 s in the dark at 18 m", text);
        }

        [Test]
        public void Percentages_DoNotDependOnTheMachineCulture()
        {
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                var enemy = Enemy(Vector3.zero, Vector3.forward, Asset<HorrorArchetype>());
                HorrorAI.EmitNoise(new Vector3(0f, 0f, -3f), 10f);
                Tick(10);

                var text = HorrorAIWindow.Describe(enemy) + HorrorAIWindow.Memory(enemy) + EnemyInspector.LiveText(enemy) + InspectorKit.Percent(0.5f);

                StringAssert.Contains("%", text);
                Assert.AreEqual("50%", InspectorKit.Percent(0.5f));
                Assert.IsFalse(text.IndexOf((char)0xA0) >= 0 || text.IndexOf((char)0x202F) >= 0, text);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Test]
        public void Menus_LiveUnderToolsHorrorAI()
        {
            var paths = new List<string>();
            foreach (var type in typeof(HorrorAIWindow).Assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (MenuItem item in method.GetCustomAttributes(typeof(MenuItem), false))
                        paths.Add(item.menuItem);
                }
            }

            Assert.That(paths, Has.Some.StartsWith("Tools/Horror AI/Open Horror AI"));
            Assert.That(paths, Has.Member("Tools/Horror AI/Documentation"));
            Assert.That(paths, Has.Member("Tools/Horror AI/GitHub Repository"));
            Assert.That(paths, Has.None.StartsWith("Window/"));
            Assert.That(paths, Has.None.Contains("%&h"));
            Assert.That(paths, Has.Member("GameObject/Horror AI/Enemy"));
            Assert.IsTrue(EditorApplication.ExecuteMenuItem("Tools/Horror AI/Open Horror AI"));
            var window = EditorWindow.GetWindow<HorrorAIWindow>();
            Assert.IsNotNull(window);
            window.Close();
        }
    }
}
