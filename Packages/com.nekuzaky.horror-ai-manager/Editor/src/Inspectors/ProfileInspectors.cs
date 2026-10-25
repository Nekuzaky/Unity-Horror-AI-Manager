using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    [CustomEditor(typeof(HorrorArchetype))]
    internal sealed class ArchetypeInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var archetype = (HorrorArchetype)target;
            var root = InspectorKit.Root("A kind of enemy: how it perceives (Perception Profile) and how it behaves (Behavior Profile). " +
                                         "Several enemies can share one archetype.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "description", "Description"));
            root.Add(InspectorKit.Field(serializedObject, "perception", "Perception"));
            root.Add(InspectorKit.Field(serializedObject, "behavior", "Behavior"));
            var summary = new Label { style = { whiteSpace = WhiteSpace.Normal, marginTop = 8f } };
            root.Add(summary);
            InspectorKit.Watch(root, serializedObject, () =>
            {
                if (archetype == null)
                    return;
                warnings.Begin();
                if (archetype.perception == null)
                    warnings.Add(HelpBoxMessageType.Warning, "No Perception Profile: the built-in default values are used.");
                if (archetype.behavior == null)
                    warnings.Add(HelpBoxMessageType.Warning, "No Behavior Profile: the built-in default values are used.");
                warnings.Render();
                summary.text = ProfileText.Summary(archetype.Perception, archetype.Behavior);
            });
            return root;
        }
    }

    [CustomEditor(typeof(PerceptionProfile))]
    internal sealed class PerceptionProfileInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var profile = (PerceptionProfile)target;
            var root = InspectorKit.Root("How an enemy sees, hears and remembers. Hover a field for an explanation.");
            var summary = new Label { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 6f, color = new Color(0.45f, 0.8f, 0.55f) } };
            root.Add(summary);
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            InspectorKit.Watch(root, serializedObject, () =>
            {
                if (profile != null)
                    summary.text = ProfileText.Sight(profile);
            });
            return root;
        }
    }

    [CustomEditor(typeof(BehaviorProfile))]
    internal sealed class BehaviorProfileInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var profile = (BehaviorProfile)target;
            var root = InspectorKit.Root("When an enemy changes its mind, and how fast it moves. Patrol, Alert, Investigate, Search, Chase, Lost Track, then back to Patrol.");
            var warnings = new Warnings();
            root.Add(warnings);
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            InspectorKit.Watch(root, serializedObject, () =>
            {
                if (profile == null)
                    return;
                warnings.Begin();
                if (profile.calmThreshold >= profile.alertThreshold)
                    warnings.Add(HelpBoxMessageType.Warning, "Calm Threshold should be below Alert Threshold, or an alert enemy calms down at once.");
                if (profile.chaseSpeed <= profile.patrolSpeed)
                    warnings.Add(HelpBoxMessageType.Info, "The enemy chases no faster than it patrols. Fine for a slow stalker, odd for anything else.");
                warnings.Render();
            });
            return root;
        }
    }

    internal static class ProfileText
    {
        public static string Sight(PerceptionProfile profile)
        {
            var ambient = HorrorAI.AmbientLight;
            var darkRate = profile.sightGain * SightMath.LightFactor(ambient, profile.darkGain);
            var reach = 6f * profile.hearingMultiplier;
            var open = reach * (1f - profile.hearingThreshold);
            var walled = profile.wallDamping > profile.hearingThreshold ? reach * (1f - profile.hearingThreshold / profile.wallDamping) : 0f;
            return "A standing target in plain view is spotted in " + Seconds(profile.sightGain) + " when lit, " + Seconds(darkRate) +
                   " in the dark close by, " + Seconds(darkRate * profile.farGain) + " in the dark at " + InspectorKit.Number(profile.viewRange, "0") +
                   " m (ambient light " + InspectorKit.Percent(ambient) + "). A footstep of loudness 6 is heard up to " + InspectorKit.Number(open, "0.#") +
                   " m in the open, " + InspectorKit.Number(walled, "0.#") + " m through one wall.";
        }

        public static string Summary(PerceptionProfile perception, BehaviorProfile behavior) =>
            Sight(perception) + "\nIt stares for " + InspectorKit.Number(behavior.alertDuration, "0.#") + " s, searches for " +
            InspectorKit.Number(behavior.searchDuration, "0") + " s within " + InspectorKit.Number(behavior.searchRadius, "0") + " m, and chases at " +
            InspectorKit.Number(behavior.chaseSpeed, "0.#") + " m/s.";

        private static string Seconds(float rate) => rate > 0f ? InspectorKit.Number(1f / rate, "0.0") + " s" : "never";
    }
}
