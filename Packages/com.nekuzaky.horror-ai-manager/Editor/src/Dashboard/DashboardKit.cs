using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal interface IDashboardView
    {
        VisualElement Root { get; }

        void Refresh(DashboardSampler sampler);

        void SceneChanged();

        void ProjectChanged();
    }

    internal sealed class StatCard
    {
        #region Public

        public readonly VisualElement Root;

        public StatCard(string glyph, string label, string detail, params string[] classes)
        {
            Root = Ui.Box("ha-card");
            for (var i = 0; i < classes.Length; i++)
                Root.AddToClassList(classes[i]);
            var head = Ui.Box("ha-card-head");
            head.Add(Glyphs.Icon(glyph, "ha-icon--small"));
            _label = Ui.Styled(label, "ha-card-label");
            head.Add(_label);
            Root.Add(head);
            _value = Ui.Styled(Text.None, "ha-card-value");
            Root.Add(_value);
            var row = Ui.Box("ha-card-head", "ha-card-foot");
            _detail = Ui.Styled(detail, "ha-card-detail");
            _extra = Ui.Styled(string.Empty, "ha-card-detail", "ha-card-extra");
            row.Add(_detail);
            row.Add(_extra);
            Root.Add(row);
        }

        public Label Label => _label;

        public Label Value => _value;

        public Label Detail => _detail;

        public Label Extra => _extra;

        public void Set(string value, string extra = null, string detail = null)
        {
            Ui.SetText(_value, value);
            Ui.SetText(_extra, extra ?? string.Empty);
            if (detail != null)
                Ui.SetText(_detail, detail);
        }

        public void SetLabel(string label) => Ui.SetText(_label, label);

        #endregion


        #region Private and Protected

        private readonly Label _label;
        private readonly Label _value;
        private readonly Label _detail;
        private readonly Label _extra;

        #endregion
    }

    internal sealed class KeyValue
    {
        #region Public

        public readonly VisualElement Root;
        public readonly Label Value;

        public KeyValue(VisualElement parent, string key)
        {
            Root = Ui.Box("ha-kv");
            Root.Add(Ui.Styled(key, "ha-kv-key"));
            Value = Ui.Styled(Text.None, "ha-kv-value");
            Root.Add(Value);
            parent.Add(Root);
        }

        public void Set(string text) => Ui.SetText(Value, text);

        #endregion
    }

    internal static class Explain
    {
        #region Public

        public static string Describe(HorrorEnemy enemy)
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

        public static string Memory(HorrorEnemy enemy)
        {
            var memory = enemy.Memory;
            return memory.HasMemory
                ? memory.Source + ", " + InspectorKit.Percent(memory.Certainty) + " sure, " + InspectorKit.Seconds(HorrorAI.Time - memory.Time) + " old"
                : "empty";
        }

        public static string Source(MemorySource source)
        {
            switch (source)
            {
                case MemorySource.Sight: return "saw it";
                case MemorySource.Hearing: return "heard it";
                case MemorySource.Scripted: return "told by a script";
                default: return "nothing";
            }
        }

        public static string FillTime(float rate) => rate > 0f ? Text.Seconds(1f / rate) : Text.None;

        public static void SelectAndFrame(Object item)
        {
            if (item == null)
                return;
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.FrameSelected();
        }

        #endregion
    }
}
