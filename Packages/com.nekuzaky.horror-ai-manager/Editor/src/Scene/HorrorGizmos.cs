using UnityEditor;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Editor
{
    [InitializeOnLoad]
    internal static class HorrorGizmos
    {
        #region Public

        public static Color ColorOf(HorrorState state)
        {
            switch (state)
            {
                case HorrorState.Alert: return new Color(1f, 0.85f, 0.2f);
                case HorrorState.Investigate: return new Color(1f, 0.6f, 0.15f);
                case HorrorState.Search: return new Color(0.75f, 0.45f, 1f);
                case HorrorState.Chase: return new Color(1f, 0.2f, 0.2f);
                case HorrorState.LostTrack: return new Color(0.95f, 0.35f, 0.6f);
                default: return new Color(0.35f, 0.85f, 0.5f);
            }
        }

        #endregion


        #region Private and Protected

        private const float FootstepLoudness = 6f;
        private static readonly Vector3[] Trail = new Vector3[HorrorBrain.TrailLength];
        private static readonly HorrorNoise[] Noises = new HorrorNoise[16];

        static HorrorGizmos()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            SceneView.duringSceneGui += OnSceneGui;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawEnemy(HorrorEnemy enemy, GizmoType type)
        {
            var selected = (type & GizmoType.Selected) != 0;
            var profile = enemy.PerceptionProfile;
            var color = Application.isPlaying && enemy.IsActive ? ColorOf(enemy.State) : ColorOf(HorrorState.Patrol);
            var eye = enemy.EyePosition;
            var forward = enemy.EyeForward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.forward;
            forward.Normalize();
            var half = Mathf.Min(profile.viewAngle, 359.9f) * 0.5f;
            var from = Quaternion.AngleAxis(-half, Vector3.up) * forward;
            Handles.color = new Color(color.r, color.g, color.b, selected ? 0.12f : 0.05f);
            Handles.DrawSolidArc(eye, Vector3.up, from, half * 2f, profile.viewRange);
            Handles.color = new Color(color.r, color.g, color.b, selected ? 0.8f : 0.35f);
            Handles.DrawWireArc(eye, Vector3.up, from, half * 2f, profile.viewRange);
            Handles.DrawLine(eye, eye + from * profile.viewRange);
            Handles.DrawLine(eye, eye + Quaternion.AngleAxis(half, Vector3.up) * forward * profile.viewRange);
            if (!selected)
                return;
            Handles.DrawWireDisc(enemy.transform.position, Vector3.up, profile.proximityRadius);
            Handles.color = new Color(0.4f, 0.7f, 1f, 0.5f);
            Handles.DrawDottedLine(enemy.transform.position, enemy.transform.position + Vector3.right * FootstepLoudness * profile.hearingMultiplier, 3f);
            Handles.DrawWireDisc(enemy.transform.position, Vector3.up, FootstepLoudness * profile.hearingMultiplier);
            if (Application.isPlaying && enemy.IsActive)
                DrawMind(enemy, color);
        }

        private static void DrawMind(HorrorEnemy enemy, Color color)
        {
            var memory = enemy.Memory;
            if (memory.HasMemory || memory.Time > 0f)
            {
                var alpha = Mathf.Lerp(0.25f, 1f, memory.Certainty);
                Handles.color = new Color(1f, 0.3f, 0.3f, alpha);
                var p = memory.Position + Vector3.up * 0.05f;
                Handles.DrawLine(p + new Vector3(-0.4f, 0f, -0.4f), p + new Vector3(0.4f, 0f, 0.4f), 3f);
                Handles.DrawLine(p + new Vector3(-0.4f, 0f, 0.4f), p + new Vector3(0.4f, 0f, -0.4f), 3f);
                Handles.DrawDottedLine(enemy.transform.position, p, 4f);
            }
            Handles.color = color;
            if (enemy.IsMoving)
                Handles.DrawLine(enemy.transform.position, enemy.Destination, 2f);
            if (enemy.State != HorrorState.Search)
                return;
            Handles.color = new Color(color.r, color.g, color.b, 0.6f);
            Handles.DrawWireDisc(enemy.SearchCenter, Vector3.up, enemy.BehaviorProfile.searchRadius);
            var count = enemy.CopySearchTrail(Trail);
            for (var i = 1; i < count; i++)
                Handles.DrawDottedLine(Trail[i - 1], Trail[i], 3f);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawRoute(PatrolRoute route, GizmoType type)
        {
            var count = route.Count;
            if (count == 0)
                return;
            var selected = (type & GizmoType.Selected) != 0;
            Handles.color = new Color(0.35f, 0.85f, 0.5f, selected ? 1f : 0.5f);
            for (var i = 0; i < count; i++)
            {
                var point = route.GetPosition(i);
                Handles.DrawWireDisc(point, Vector3.up, 0.3f);
                if (i > 0)
                    Handles.DrawLine(route.GetPosition(i - 1), point, 2f);
                if (selected)
                    Handles.Label(point + Vector3.up * 0.4f, (i + 1) + (route.GetWait(i) > 0f ? $"  wait {route.GetWait(i):0.#} s" : string.Empty));
            }
            if (route.mode == PatrolMode.Loop && count > 2)
                Handles.DrawDottedLine(route.GetPosition(count - 1), route.GetPosition(0), 4f);
        }

        [DrawGizmo(GizmoType.Selected)]
        private static void DrawLight(HorrorLightSource light, GizmoType type)
        {
            Handles.color = new Color(1f, 0.9f, 0.4f, light.IsOn || !Application.isPlaying ? 0.6f : 0.15f);
            var position = light.transform.position;
            if (!light.IsSpot)
            {
                Handles.DrawWireDisc(position, Vector3.up, light.Range);
                return;
            }
            var forward = light.transform.forward;
            var radius = Mathf.Tan(light.SpotAngle * 0.5f * Mathf.Deg2Rad) * light.Range;
            var end = position + forward * light.Range;
            Handles.DrawWireDisc(end, forward, radius);
            var right = Vector3.Cross(forward, Mathf.Abs(forward.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            var up = Vector3.Cross(right, forward);
            Handles.DrawLine(position, end + right * radius);
            Handles.DrawLine(position, end - right * radius);
            Handles.DrawLine(position, end + up * radius);
            Handles.DrawLine(position, end - up * radius);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawTarget(HorrorTarget target, GizmoType type)
        {
            var lit = Application.isPlaying ? target.Exposure : 0.5f;
            Handles.color = Color.Lerp(new Color(0.3f, 0.3f, 0.5f), new Color(1f, 0.95f, 0.6f), lit);
            var count = target.SampleCount;
            for (var i = 0; i < count; i++)
                Handles.DrawWireDisc(target.GetSamplePoint(i), Camera.current != null ? Camera.current.transform.forward : Vector3.forward, 0.08f);
        }

        private static void OnSceneGui(SceneView view)
        {
            if (!Application.isPlaying || Event.current.type != EventType.Repaint)
                return;
            var enemies = HorrorAI.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                    DrawBar(enemies[i]);
            }
            DrawNoises();
        }

        private static void DrawBar(HorrorEnemy enemy)
        {
            var head = enemy.transform.position + Vector3.up * (enemy.PerceptionProfile.eyeHeight + 0.6f);
            var gui = HandleUtility.WorldToGUIPointWithDepth(head);
            if (gui.z < 0f)
                return;
            Handles.BeginGUI();
            var rect = new Rect(gui.x - 30f, gui.y - 4f, 60f, 7f);
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.6f));
            var fill = rect;
            fill.width *= Mathf.Clamp01(enemy.Suspicion);
            EditorGUI.DrawRect(fill, ColorOf(enemy.State));
            GUI.Label(new Rect(gui.x - 60f, gui.y - 22f, 120f, 18f), enemy.State.ToString(), EditorStyles.centeredGreyMiniLabel);
            Handles.EndGUI();
        }

        private static void DrawNoises()
        {
            var count = HorrorAI.CopyRecentNoises(Noises);
            var now = HorrorAI.Time;
            for (var i = 0; i < count; i++)
            {
                var age = now - Noises[i].Time;
                if (age > 1.5f || age < 0f)
                    continue;
                Handles.color = new Color(0.4f, 0.7f, 1f, 1f - age / 1.5f);
                Handles.DrawWireDisc(Noises[i].Position, Vector3.up, Noises[i].Loudness * Mathf.Clamp01(age / 0.6f));
            }
        }

        #endregion
    }
}
