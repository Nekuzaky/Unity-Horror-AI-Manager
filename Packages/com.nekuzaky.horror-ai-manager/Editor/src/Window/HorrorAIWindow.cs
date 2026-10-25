using UnityEditor;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class HorrorAIWindow : EditorWindow
    {
        #region Public

        [MenuItem("Window/Horror AI", false, 2000)]
        public static void Open()
        {
            var window = GetWindow<HorrorAIWindow>();
            window.titleContent = new GUIContent("Horror AI");
            window.minSize = new Vector2(520f, 200f);
            window.Show();
        }

        internal static string Describe(HorrorEnemy enemy)
        {
            var perception = enemy.Perception;
            var sees = perception.Sees ? $"sees {perception.VisibleFraction:P0} of the target, lit {perception.Exposure:P0}" : "sees nothing";
            var heard = perception.HeardTime > 0f ? $"heard a noise {HorrorAI.Time - perception.HeardTime:0.0} s ago ({perception.HeardLoudness:P0})" : "heard nothing";
            return $"{sees}; {heard}";
        }

        internal static string Memory(HorrorEnemy enemy)
        {
            var memory = enemy.Memory;
            return memory.HasMemory ? $"{memory.Source}, {memory.Certainty:P0} sure, {HorrorAI.Time - memory.Time:0.0} s old" : "empty";
        }

        #endregion


        #region Private and Protected

        private Vector2 _scroll;
        private double _nextRepaint;

        private void OnEnable() => EditorApplication.update += Tick;

        private void OnDisable() => EditorApplication.update -= Tick;

        private void Tick()
        {
            if (!Application.isPlaying || EditorApplication.timeSinceStartup < _nextRepaint)
                return;
            _nextRepaint = EditorApplication.timeSinceStartup + 0.1d;
            Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                DrawEditMode();
                return;
            }
            var stats = HorrorAI.Stats;
            EditorGUILayout.LabelField($"{stats.Enemies} enemies, {stats.Targets} targets, {stats.Lights} lights   |   " +
                                       $"{stats.Milliseconds:0.000} ms, {stats.Perceptions} perceptions, {stats.Raycasts}/{HorrorAI.RaycastBudget} rays, " +
                                       $"{stats.Deferred} deferred, {stats.AllocatedBytes} B allocated this frame", EditorStyles.miniLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var enemies = HorrorAI.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                    DrawEnemy(enemies[i]);
            }
            if (enemies.Count == 0)
                EditorGUILayout.HelpBox("No enemy is running. Add a Horror Enemy to the scene.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void DrawEnemy(HorrorEnemy enemy)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            var color = GUI.color;
            GUI.color = HorrorGizmos.ColorOf(enemy.State);
            if (GUILayout.Button($"{enemy.name}  —  {enemy.State}", EditorStyles.boldLabel, GUILayout.Width(220f)))
            {
                Selection.activeGameObject = enemy.gameObject;
                EditorGUIUtility.PingObject(enemy.gameObject);
            }
            GUI.color = color;
            var bar = GUILayoutUtility.GetRect(100f, 16f, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(bar, enemy.Suspicion, $"Suspicion {enemy.Suspicion:P0}");
            EditorGUILayout.EndHorizontal();
            var transition = enemy.LastTransition;
            EditorGUILayout.LabelField($"Last change: {transition.From} → {transition.To} because it {transition.Explanation}, {HorrorAI.Time - transition.Time:0.0} s ago",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Now: " + Describe(enemy), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Memory: " + Memory(enemy), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private static void DrawEditMode()
        {
            EditorGUILayout.HelpBox("Press Play to watch every enemy live: its state, suspicion, what it sees and hears, and why it last changed its mind.", MessageType.Info);
            foreach (var enemy in FindObjectsByType<HorrorEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var warnings = new Warnings();
                EnemyInspector.Collect(enemy, warnings);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(enemy.name, EditorStyles.linkLabel, GUILayout.Width(200f)))
                    Selection.activeGameObject = enemy.gameObject;
                EditorGUILayout.LabelField(warnings.Count == 0 ? "Ready" : warnings.Count + " thing(s) to check in its inspector", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion
    }
}
