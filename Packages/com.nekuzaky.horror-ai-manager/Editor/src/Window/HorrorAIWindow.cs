using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class HorrorAIWindow : EditorWindow
    {
        #region Public

        public const string MenuRoot = "Tools/Horror AI/";
        public const string DocumentationUrl = "https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/index.md";
        public const string RepositoryUrl = "https://github.com/Nekuzaky/Unity-Horror-AI-Manager";

        [MenuItem(MenuRoot + "Open Horror AI %#&h", priority = 1)]
        public static void Open()
        {
            var window = GetWindow<HorrorAIWindow>();
            window.titleContent = new GUIContent("Horror AI");
            window.minSize = new Vector2(520f, 200f);
            window.Show();
        }

        [MenuItem(MenuRoot + "Documentation", priority = 40)]
        public static void OpenDocumentation() => Application.OpenURL(DocumentationUrl);

        [MenuItem(MenuRoot + "GitHub Repository", priority = 41)]
        public static void OpenRepository() => Application.OpenURL(RepositoryUrl);

        internal static string Describe(HorrorEnemy enemy)
        {
            var perception = enemy.Perception;
            var sees = perception.Sees
                ? "sees " + InspectorKit.Percent(perception.VisibleFraction) + " of the target, lit " + InspectorKit.Percent(perception.Exposure)
                : "sees nothing";
            var heard = perception.HeardTime > 0f
                ? "heard a noise " + InspectorKit.Seconds(HorrorAI.Time - perception.HeardTime) + " ago (" + InspectorKit.Percent(perception.HeardLoudness) + ")"
                : "heard nothing";
            return sees + "; " + heard;
        }

        internal static string Memory(HorrorEnemy enemy)
        {
            var memory = enemy.Memory;
            return memory.HasMemory
                ? memory.Source + ", " + InspectorKit.Percent(memory.Certainty) + " sure, " + InspectorKit.Seconds(HorrorAI.Time - memory.Time) + " old"
                : "empty";
        }

        internal static string Allocated(long bytes) => bytes < 0L ? "allocation not measured" : bytes + " B allocated by the update";

        #endregion


        #region Private and Protected

        private Vector2 _scroll;
        private double _nextRepaint;

        private void OnEnable()
        {
            EditorApplication.update += Tick;
            EditorApplication.hierarchyChanged += Repaint;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            EditorApplication.hierarchyChanged -= Repaint;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) => Repaint();

        private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene from, UnityEngine.SceneManagement.Scene to) => Repaint();

        private void Tick()
        {
            if (EditorApplication.timeSinceStartup < _nextRepaint)
                return;
            _nextRepaint = EditorApplication.timeSinceStartup + (Application.isPlaying ? 0.1d : 1d);
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
                                       $"{InspectorKit.Milliseconds(stats.Milliseconds)}, {stats.Perceptions} perceptions, {stats.Raycasts}/{HorrorAI.RaycastBudget} rays, " +
                                       $"{stats.Deferred} deferred, {Allocated(stats.AllocatedBytes)}", EditorStyles.miniLabel);
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
            EditorGUI.ProgressBar(bar, enemy.Suspicion, "Suspicion " + InspectorKit.Percent(enemy.Suspicion));
            EditorGUILayout.EndHorizontal();
            var transition = enemy.LastTransition;
            EditorGUILayout.LabelField($"Last change: {transition.From} → {transition.To} because it {transition.Explanation}, {InspectorKit.Seconds(HorrorAI.Time - transition.Time)} ago",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Now: " + Describe(enemy), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Memory: " + Memory(enemy), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private static void DrawEditMode()
        {
            EditorGUILayout.HelpBox("Press Play to watch every enemy live: its state, suspicion, what it sees and hears, and why it last changed its mind.", MessageType.Info);
            var enemies = FindObjectsByType<HorrorEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            if (enemies.Length == 0)
                EditorGUILayout.LabelField("No Horror Enemy in the open scenes. Add one with GameObject > Horror AI > Enemy.", EditorStyles.wordWrappedMiniLabel);
            foreach (var enemy in enemies)
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
