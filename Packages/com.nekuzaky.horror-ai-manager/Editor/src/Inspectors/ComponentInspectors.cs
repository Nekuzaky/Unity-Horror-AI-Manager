using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    [CustomEditor(typeof(HorrorTarget))]
    [CanEditMultipleObjects]
    internal sealed class TargetInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var horrorTarget = (HorrorTarget)target;
            var root = InspectorKit.Root("What enemies look for: usually the player. Darkness and crouching make it harder to notice.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "crouching", "Crouching"));
            root.Add(InspectorKit.Field(serializedObject, "visibilityMultiplier", "Visibility"));
            root.Add(InspectorKit.Field(serializedObject, "crouchVisibility", "Crouch Visibility"));
            root.Add(InspectorKit.Field(serializedObject, "crouchHeight", "Crouch Height"));
            root.Add(InspectorKit.Field(serializedObject, "samplePoints", "Body Points"));
            root.Add(InspectorKit.Field(serializedObject, "occluders", "Light Blockers"));
            root.Add(InspectorKit.Live(() => horrorTarget == null ? string.Empty :
                "Lit " + InspectorKit.Percent(horrorTarget.Exposure) + "   Visibility " + InspectorKit.Percent(horrorTarget.Visibility) +
                (horrorTarget.Crouching ? "   (crouching)" : string.Empty)));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                warnings.Begin();
                if (SceneFixes.CountTargets() > 1)
                    warnings.Add(HelpBoxMessageType.Info, "There are several Horror Targets. Each enemy watches the four first ones and chases the most visible.");
                if (SceneFixes.CountEnemies() == 0)
                    warnings.Add(HelpBoxMessageType.Info, "No Horror Enemy in the scene yet. Add one with GameObject > Horror AI > Enemy.");
                if (SceneFixes.CountLights() == 0)
                    warnings.Add(HelpBoxMessageType.Info, "No Horror Light Source in the scene: this target always counts as standing in the dark (ambient light only).",
                        SceneFixes.CountUnmarkedLights() > 0 ? "Mark every light" : null, SceneFixes.CountUnmarkedLights() > 0 ? SceneFixes.MarkAllLights : null);
                if (horrorTarget != null && horrorTarget.samplePoints != null && horrorTarget.samplePoints.Length > HorrorWorld.MaxPointsPerTarget)
                    warnings.Add(HelpBoxMessageType.Warning, "Only the first four body points are used.");
                warnings.Render();
            });
            return root;
        }
    }

    [CustomEditor(typeof(HorrorLightSource))]
    [CanEditMultipleObjects]
    internal sealed class LightSourceInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var source = (HorrorLightSource)target;
            var root = InspectorKit.Root("Tells the AI that this light reveals what it shines on. Targets in its range and beam are spotted faster.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "linkedLight", "Light"));
            root.Add(InspectorKit.Field(serializedObject, "followLight", "Follow Its Settings"));
            var manual = new VisualElement();
            manual.Add(InspectorKit.Field(serializedObject, "shape", "Shape"));
            manual.Add(InspectorKit.Field(serializedObject, "range", "Range"));
            manual.Add(InspectorKit.Field(serializedObject, "spotAngle", "Spot Angle"));
            root.Add(manual);
            root.Add(InspectorKit.Field(serializedObject, "intensity", "Reveal Strength"));
            root.Add(InspectorKit.Field(serializedObject, "_on", "On"));
            root.Add(InspectorKit.Field(serializedObject, "revealsHolder", "Reveals Its Holder"));
            root.Add(InspectorKit.Field(serializedObject, "holderExposure", "Holder Exposure"));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                if (source == null)
                    return;
                manual.style.display = source.followLight && source.linkedLight != null ? DisplayStyle.None : DisplayStyle.Flex;
                warnings.Begin();
                var light = source.GetComponent<Light>();
                if (source.linkedLight == null && light != null)
                    warnings.Add(HelpBoxMessageType.Info, "There is a Light on this object. Link it so turning it off also hides the target.",
                        "Link the Light", () =>
                        {
                            Undo.RecordObject(source, "Link Light");
                            source.linkedLight = light;
                        });
                if (source.IsDirectional)
                    warnings.Add(HelpBoxMessageType.Info, "A directional light (sun, moon) lights every target that has open sky in its direction, wherever it stands. " +
                                                          "Keep Reveal Strength low for moonlight (0.1 to 0.3).");
                if (source.revealsHolder && source.Holder == null)
                    warnings.Add(HelpBoxMessageType.Warning, "Reveals Its Holder is on, but no parent has a Horror Target. Put this light under the player.");
                warnings.Render();
            });
            return root;
        }
    }

    [CustomEditor(typeof(NoiseEmitter))]
    [CanEditMultipleObjects]
    internal sealed class NoiseEmitterInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var emitter = (NoiseEmitter)target;
            var root = InspectorKit.Root("Makes a noise enemies can hear: when this object hits something, enters a trigger, or when an inspector event or an animation event calls Emit.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "loudness", "Loudness (m)"));
            root.Add(InspectorKit.Field(serializedObject, "emitOn", "Make Noise When"));
            root.Add(InspectorKit.Field(serializedObject, "minImpactSpeed", "Min Impact Speed"));
            root.Add(InspectorKit.Field(serializedObject, "fullImpactSpeed", "Full Impact Speed"));
            root.Add(InspectorKit.Field(serializedObject, "cooldown", "Cooldown (s)"));
            root.Add(InspectorKit.Field(serializedObject, "triggerTag", "Only For Tag"));
            root.Add(InspectorKit.Button("Make the noise now", () => emitter.Emit(), "Works in Play Mode"));
            InspectorKit.Watch(root, serializedObject, () => Validate(emitter, warnings));
            return root;
        }

        private static void Validate(NoiseEmitter emitter, Warnings warnings)
        {
            if (emitter == null)
                return;
            warnings.Begin();
            var go = emitter.gameObject;
            var collider = go.GetComponent<Collider>();
            var needsCollider = (emitter.emitOn & (NoiseTrigger.CollisionEnter | NoiseTrigger.TriggerEnter)) != 0;
            if (needsCollider && collider == null)
                warnings.Add(HelpBoxMessageType.Warning, "Collisions and triggers need a collider on this object.", "Add a Box Collider",
                    () => Undo.AddComponent<BoxCollider>(go).isTrigger = (emitter.emitOn & NoiseTrigger.TriggerEnter) != 0);
            if ((emitter.emitOn & NoiseTrigger.TriggerEnter) != 0 && collider != null && !collider.isTrigger)
                warnings.Add(HelpBoxMessageType.Warning, "Trigger Enter only fires when the collider is a trigger.", "Make it a trigger", () =>
                {
                    Undo.RecordObject(collider, "Make trigger");
                    collider.isTrigger = true;
                });
            if ((emitter.emitOn & NoiseTrigger.CollisionEnter) != 0 && go.GetComponent<Rigidbody>() == null)
                warnings.Add(HelpBoxMessageType.Info, "Collisions need a Rigidbody on this object or on what hits it. Props that fall or get kicked need one here.",
                    "Add a Rigidbody", () => Undo.AddComponent<Rigidbody>(go));
            if (emitter.emitOn == NoiseTrigger.None)
                warnings.Add(HelpBoxMessageType.Info, "Make Noise When is empty: call Emit() from an event in the inspector, an animation event or a script.");
            warnings.Render();
        }
    }

    [CustomEditor(typeof(PatrolRoute))]
    internal sealed class PatrolRouteInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var route = (PatrolRoute)target;
            var root = InspectorKit.Root("The path an enemy walks while nothing is wrong. Each waypoint is a child object; the enemy waits there for the given seconds.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "mode", "Mode"));
            root.Add(InspectorKit.Field(serializedObject, "waypoints", "Waypoints"));
            root.Add(InspectorKit.Button("Add a waypoint", () => AddWaypoint(route)));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                if (route == null)
                    return;
                warnings.Begin();
                if (route.Count < 2)
                    warnings.Add(HelpBoxMessageType.Warning, "Add at least two waypoints, or the enemy only stands still.", "Add a waypoint", () => AddWaypoint(route));
                for (var i = 0; i < route.Count; i++)
                {
                    if (route.waypoints[i].point == null)
                        warnings.Add(HelpBoxMessageType.Error, $"Waypoint {i + 1} has no transform.");
                    else if (!SceneFixes.IsOnNavMesh(route.GetPosition(i)))
                        warnings.Add(HelpBoxMessageType.Warning, $"Waypoint {i + 1} is not on the NavMesh, so the enemy cannot reach it.", "Bake a NavMesh", SceneFixes.BakeNavMesh);
                }
                warnings.Render();
            });
            return root;
        }

        private static void AddWaypoint(PatrolRoute route)
        {
            Undo.RecordObject(route, "Add waypoint");
            var last = route.Count > 0 ? route.GetPosition(route.Count - 1) : route.transform.position;
            var point = route.AddWaypoint(last + route.transform.forward * 4f, 1.5f);
            Undo.RegisterCreatedObjectUndo(point.gameObject, "Add waypoint");
            Selection.activeTransform = point;
        }
    }
}
