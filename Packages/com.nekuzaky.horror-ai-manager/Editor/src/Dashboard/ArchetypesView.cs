using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class ArchetypesView : IDashboardView
    {
        #region Public

        public VisualElement Root { get; }

        public ArchetypesView()
        {
            Root = Ui.Box("ha-view");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("ha-scroll");
            var page = Ui.Box("ha-page");
            scroll.Add(page);
            Root.Add(scroll);
            page.Add(Ui.Styled("The kinds of enemies in this project: how each one perceives and behaves, in plain words. Click one to select it.", "ha-help"));
            var toolbar = Ui.Box("ha-toolbar");
            toolbar.Add(Ui.TextButton(Glyphs.Add, "New archetype from the Patroller", NewArchetype, "A copy of the Patroller preset with its own profiles"));
            toolbar.Add(Ui.TextButton(Glyphs.Add, "New perception profile", () => NewProfile<PerceptionProfile>("Perception Profile")));
            toolbar.Add(Ui.TextButton(Glyphs.Add, "New behavior profile", () => NewProfile<BehaviorProfile>("Behavior Profile")));
            page.Add(toolbar);
            _archetypes = Group(page, "HorrorArchetype", "Archetypes");
            _perception = Group(page, "PerceptionProfile", "Perception profiles");
            _behavior = Group(page, "BehaviorProfile", "Behavior profiles");
        }

        public int Count => _archetypes.childCount + _perception.childCount + _behavior.childCount;

        public void Refresh(DashboardSampler sampler)
        {
            if (_dirty)
                Rebuild();
        }

        public void Rebuild()
        {
            _dirty = false;
            Fill<HorrorArchetype>(_archetypes, "HorrorArchetype", a => ProfileText.Summary(a.Perception, a.Behavior) + Users(a));
            Fill<PerceptionProfile>(_perception, "PerceptionProfile", ProfileText.Sight);
            Fill<BehaviorProfile>(_behavior, "BehaviorProfile", ProfileText.Behavior);
        }

        public void SceneChanged() => _dirty = true;

        public void ProjectChanged() => _dirty = true;

        public static HorrorArchetype CreateFromPreset(string path)
        {
            var preset = SceneFixes.Patroller;
            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var stem = Path.GetFileNameWithoutExtension(path);
            var archetype = ScriptableObject.CreateInstance<HorrorArchetype>();
            archetype.description = preset != null ? preset.description : string.Empty;
            archetype.perception = Copy(preset != null ? preset.Perception : null, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + stem + " Perception.asset"));
            archetype.behavior = Copy(preset != null ? preset.Behavior : null, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + stem + " Behavior.asset"));
            AssetDatabase.CreateAsset(archetype, path);
            AssetDatabase.SaveAssets();
            return archetype;
        }

        #endregion


        #region Private and Protected

        private readonly VisualElement _archetypes;
        private readonly VisualElement _perception;
        private readonly VisualElement _behavior;
        private bool _dirty = true;

        private static VisualElement Group(VisualElement page, string tile, string title)
        {
            var section = Ui.Section(null, title);
            section.Insert(0, Glyphs.TileImage(tile, "ha-tile"));
            page.Add(section);
            var list = Ui.Box();
            page.Add(list);
            return list;
        }

        private static void Fill<T>(VisualElement list, string tile, System.Func<T, string> summary) where T : ScriptableObject
        {
            list.Clear();
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    list.Add(Card(asset, path, tile, summary(asset)));
            }
            if (guids.Length == 0)
                list.Add(Ui.Styled("None yet.", "ha-help"));
        }

        private static VisualElement Card(Object asset, string path, string tile, string summary)
        {
            var card = Ui.Box("ha-asset");
            card.Add(Glyphs.TileImage(tile, "ha-tile--large"));
            var text = Ui.Box("ha-asset-text");
            text.Add(Ui.Styled(asset.name, "ha-asset-name"));
            text.Add(Ui.Styled(path, "ha-asset-path"));
            text.Add(Ui.Styled(summary, "ha-asset-summary"));
            card.Add(text);
            card.RegisterCallback<ClickEvent>(_ =>
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            });
            return card;
        }

        private static string Users(HorrorArchetype archetype)
        {
            var count = 0;
            foreach (var enemy in Object.FindObjectsByType<HorrorEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (enemy.archetype == archetype)
                    count++;
            }
            return count == 0 ? "\nNo enemy in the open scenes uses it." : count == 1 ? "\nUsed by 1 enemy in the open scenes." : "\nUsed by " + count + " enemies in the open scenes.";
        }

        private static T Copy<T>(T source, string path) where T : ScriptableObject
        {
            var copy = source != null ? Object.Instantiate(source) : ScriptableObject.CreateInstance<T>();
            copy.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        private void NewArchetype()
        {
            var path = EditorUtility.SaveFilePanelInProject("New archetype", "Archetype", "asset", "Where to save the archetype. Its two profiles are saved next to it.");
            if (string.IsNullOrEmpty(path))
                return;
            var archetype = CreateFromPreset(path);
            Selection.activeObject = archetype;
            EditorGUIUtility.PingObject(archetype);
            Rebuild();
        }

        private void NewProfile<T>(string name) where T : ScriptableObject
        {
            var path = EditorUtility.SaveFilePanelInProject("New " + name.ToLowerInvariant(), name, "asset", "Where to save the profile.");
            if (string.IsNullOrEmpty(path))
                return;
            var profile = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
            Rebuild();
        }

        #endregion
    }
}
