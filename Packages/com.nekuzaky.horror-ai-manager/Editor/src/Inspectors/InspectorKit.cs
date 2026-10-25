using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class Warnings : VisualElement
    {
        #region Public

        public int Count => _items.Count;

        public void Begin() => _items.Clear();

        public void Add(HelpBoxMessageType type, string message, string fix = null, Action action = null) =>
            _items.Add(new Item { Type = type, Message = message, Fix = fix, Action = action });

        public bool Contains(string fragment)
        {
            foreach (var item in _items)
            {
                if (item.Message.Contains(fragment))
                    return true;
            }
            return false;
        }

        public void Render()
        {
            var key = string.Empty;
            foreach (var item in _items)
                key += item.Type + "|" + item.Message + "|" + item.Fix + ";";
            if (key == _shown)
                return;
            _shown = key;
            Clear();
            foreach (var item in _items)
            {
                var box = new HelpBox(item.Message, item.Type);
                if (item.Action != null)
                    box.Add(new Button(item.Action) { text = item.Fix });
                Add(box);
            }
        }

        #endregion


        #region Private and Protected

        private struct Item
        {
            public HelpBoxMessageType Type;
            public string Message;
            public string Fix;
            public Action Action;
        }

        private readonly List<Item> _items = new();
        private string _shown;

        #endregion
    }

    internal static class InspectorKit
    {
        #region Public

        public static VisualElement Root(string summary)
        {
            var root = new VisualElement();
            var label = new Label(summary)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 6f,
                    marginTop = 2f,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    color = new Color(0.72f, 0.72f, 0.72f)
                }
            };
            root.Add(label);
            return root;
        }

        public static Label Section(string title) => new(title)
        {
            style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8f, marginBottom = 2f }
        };

        public static PropertyField Field(SerializedObject serialized, string path, string label = null)
        {
            var property = serialized.FindProperty(path);
            return label == null ? new PropertyField(property) : new PropertyField(property, label);
        }

        public static Label Live(Func<string> text)
        {
            var label = new Label { style = { color = new Color(0.45f, 0.8f, 0.55f), marginTop = 6f, whiteSpace = WhiteSpace.Normal } };
            label.schedule.Execute(() =>
            {
                label.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
                if (Application.isPlaying)
                    label.text = text();
            }).Every(150);
            return label;
        }

        public static void Watch(VisualElement root, SerializedObject serialized, Action validate)
        {
            root.TrackSerializedObjectValue(serialized, _ => validate());
            root.schedule.Execute(validate).Every(1000);
            validate();
        }

        public static string Percent(float value) => Mathf.RoundToInt(value * 100f).ToString(CultureInfo.InvariantCulture) + "%";

        public static string Seconds(float value) => value.ToString("0.0", CultureInfo.InvariantCulture) + " s";

        public static string Milliseconds(float value) => value.ToString("0.000", CultureInfo.InvariantCulture) + " ms";

        public static string Number(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        public static Button Button(string text, Action click, string tooltip = null) => new(click) { text = text, tooltip = tooltip };

        #endregion
    }
}
