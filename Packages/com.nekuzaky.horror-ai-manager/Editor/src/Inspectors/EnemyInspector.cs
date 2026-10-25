using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    [CustomEditor(typeof(HorrorEnemy))]
    [CanEditMultipleObjects]
    internal sealed class EnemyInspector : UnityEditor.Editor
    {
        #region Public

        public override VisualElement CreateInspectorGUI()
        {
            var enemy = (HorrorEnemy)target;
            var root = InspectorKit.Root("An enemy that sees, hears and remembers. It patrols, stops when something feels wrong, " +
                                         "checks it out, searches, and chases what it clearly sees.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "archetype", "Archetype"));
            root.Add(InspectorKit.Field(serializedObject, "patrolRoute", "Patrol Route"));
            root.Add(InspectorKit.Field(serializedObject, "eye", "Eye (optional)"));
            root.Add(InspectorKit.Field(serializedObject, "seed", "Search Seed"));
            root.Add(InspectorKit.Live(() => LiveText(enemy)));
            root.Add(EventsFoldout());
            InspectorKit.Watch(root, serializedObject, () => Validate(enemy, warnings));
            return root;
        }

        internal static void Collect(HorrorEnemy enemy, Warnings warnings)
        {
            if (enemy.archetype == null)
                warnings.Add(HelpBoxMessageType.Warning, "No archetype: the enemy uses the built-in default values. Pick one, or start from the Patroller preset.",
                    "Use the Patroller preset", () => SceneFixes.UsePatroller(enemy));
            var agent = enemy.GetComponent<NavMeshAgent>();
            if (agent == null)
                warnings.Add(HelpBoxMessageType.Warning, "No NavMesh Agent: the enemy will slide in straight lines through walls. Add one so it walks around obstacles.",
                    "Add a NavMesh Agent", () => SceneFixes.AddAgent(enemy));
            else if (!SceneFixes.IsOnNavMesh(enemy.transform.position))
                warnings.Add(HelpBoxMessageType.Error, "There is no NavMesh under this enemy, so it cannot walk. Bake one for the level.",
                    "Bake a NavMesh", SceneFixes.BakeNavMesh);
            if (SceneFixes.CountTargets() == 0)
                warnings.Add(HelpBoxMessageType.Error, "There is no Horror Target in the scene: the enemy has nobody to see. Put one on the player.",
                    "Add a Horror Target to the player", () => SceneFixes.AddTarget());
            if (enemy.patrolRoute == null)
                warnings.Add(HelpBoxMessageType.Info, "No patrol route: it stands guard where it starts and comes back there after a search.",
                    "Create a patrol route", () => SceneFixes.CreateRoute(enemy));
            else if (enemy.patrolRoute.Count < 2)
                warnings.Add(HelpBoxMessageType.Warning, "The patrol route has fewer than two waypoints, so the enemy does not walk anywhere.");
            if (SceneFixes.CountLights() == 0 && SceneFixes.CountUnmarkedLights() > 0)
                warnings.Add(HelpBoxMessageType.Info, "No light is marked for the AI: every target counts as standing in the dark. Mark the lamps that should make the player easier to spot.",
                    "Mark every light", SceneFixes.MarkAllLights);
        }

        #endregion


        #region Private and Protected

        private static void Validate(HorrorEnemy enemy, Warnings warnings)
        {
            if (enemy == null)
                return;
            warnings.Begin();
            Collect(enemy, warnings);
            warnings.Render();
        }

        private VisualElement EventsFoldout()
        {
            var foldout = new Foldout { text = "Events (sounds, music, scares)", value = false };
            foldout.Add(new Label("Hook sounds, music and jump scares here, no code needed.") { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 4f } });
            foldout.Add(InspectorKit.Field(serializedObject, "onSpotted", "On Spotted"));
            foldout.Add(InspectorKit.Field(serializedObject, "onChaseStarted", "On Chase Started"));
            foldout.Add(InspectorKit.Field(serializedObject, "onCaught", "On Caught"));
            foldout.Add(InspectorKit.Field(serializedObject, "onLostTrack", "On Lost Track"));
            foldout.Add(InspectorKit.Field(serializedObject, "onSearchStarted", "On Search Started"));
            foldout.Add(InspectorKit.Field(serializedObject, "onGaveUp", "On Gave Up"));
            foldout.Add(InspectorKit.Field(serializedObject, "onAlerted", "On Alerted"));
            foldout.Add(InspectorKit.Field(serializedObject, "onInvestigate", "On Investigate"));
            foldout.Add(InspectorKit.Field(serializedObject, "onNoiseHeard", "On Noise Heard"));
            foldout.Add(InspectorKit.Field(serializedObject, "onStateChanged", "On State Changed"));
            return foldout;
        }

        internal static string LiveText(HorrorEnemy enemy)
        {
            if (enemy == null || !enemy.IsActive)
                return "Not running";
            var perception = enemy.Perception;
            var memory = enemy.Memory;
            return enemy.State + " for " + InspectorKit.Seconds(enemy.StateTime) + " (" + enemy.LastTransition.Explanation + ")\n" +
                   "Suspicion " + InspectorKit.Percent(enemy.Suspicion) + "   Sees " + (perception.Sees ? InspectorKit.Percent(perception.VisibleFraction) : "nothing") +
                   "   Target lit " + InspectorKit.Percent(perception.Exposure) + "\n" +
                   "Memory " + (memory.HasMemory
                       ? memory.Source + ", " + InspectorKit.Percent(memory.Certainty) + " sure, " + InspectorKit.Seconds(HorrorAI.Time - memory.Time) + " ago"
                       : "empty");
        }

        #endregion
    }
}
