using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal static class Ui
    {
        #region Public

        public static Button IconButton(string glyph, string tooltip, Action click, params string[] classes)
        {
            var button = new Button(click) { tooltip = tooltip };
            button.AddToClassList("ha-button");
            button.AddToClassList("ha-button--icon");
            button.Add(Glyphs.Icon(glyph));
            for (var i = 0; i < classes.Length; i++)
                button.AddToClassList(classes[i]);
            return button;
        }

        public static Button TextButton(string glyph, string text, Action click, string tooltip = null)
        {
            var button = new Button(click) { tooltip = tooltip };
            button.AddToClassList("ha-button");
            var label = new Label(text);
            if (!string.IsNullOrEmpty(glyph))
            {
                button.Add(Glyphs.Icon(glyph));
                label.AddToClassList("ha-button-label");
            }
            button.Add(label);
            return button;
        }

        public static VisualElement Section(string glyph, string text)
        {
            var title = Box("ha-section");
            if (!string.IsNullOrEmpty(glyph))
                title.Add(Glyphs.Icon(glyph, "ha-icon--small", "ha-icon--dim"));
            title.Add(new Label(text.ToUpperInvariant()));
            return title;
        }

        public static Label Styled(string text, params string[] classes)
        {
            var label = new Label(text);
            for (var i = 0; i < classes.Length; i++)
                label.AddToClassList(classes[i]);
            return label;
        }

        public static VisualElement Box(params string[] classes)
        {
            var box = new VisualElement();
            for (var i = 0; i < classes.Length; i++)
                box.AddToClassList(classes[i]);
            return box;
        }

        public static VisualElement Spacer() => Box("ha-spacer");

        public static VisualElement Empty(string title, string text)
        {
            var empty = Box("ha-empty");
            empty.Add(Styled(title, "ha-empty-title"));
            empty.Add(Styled(text, "ha-empty-text"));
            return empty;
        }

        public static void SetText(Label label, string text)
        {
            if (!ReferenceEquals(label.text, text) && label.text != text)
                label.text = text;
        }

        public static void SetVisible(VisualElement element, bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            var current = element.style.display;
            if (current.keyword == StyleKeyword.Undefined && current.value == display)
                return;
            element.style.display = display;
        }

        public static void SwapClass(VisualElement element, ref string current, string next)
        {
            if (current == next)
                return;
            if (current != null)
                element.RemoveFromClassList(current);
            if (next != null)
                element.AddToClassList(next);
            current = next;
        }

        public static string StateClass(HorrorState state) => StateClasses[(int)state];

        public static string StateName(HorrorState state) => StateNames[(int)state];

        public static readonly string[] StateClasses =
        {
            "ha-state--patrol", "ha-state--alert", "ha-state--investigate", "ha-state--search", "ha-state--chase", "ha-state--losttrack"
        };

        public static readonly string[] StateNames = { "Patrol", "Alert", "Investigate", "Search", "Chase", "Lost Track" };

        #endregion
    }

    internal static class Text
    {
        #region Public

        public const string None = "-";
        public const string NotMeasured = "n/a";

        public static string Percent(float value)
        {
            var index = Mathf.Clamp(Mathf.RoundToInt(value * 100f), 0, 100);
            return PercentCache[index] ??= index.ToString(CultureInfo.InvariantCulture) + "%";
        }

        public static string Integer(int value)
        {
            if (value < 0 || value >= IntegerCache.Length)
                return value.ToString(CultureInfo.InvariantCulture);
            return IntegerCache[value] ??= value.ToString(CultureInfo.InvariantCulture);
        }

        public static string Milliseconds(float value) => Cached(Milli, value, 1000f, "0.000", " ms");

        public static string Seconds(float value) => Cached(SecondsCache, value, 10f, "0.0", " s");

        public static string Ago(float value) => Cached(AgoCache, value, 10f, "0.0", " s ago");

        public static string Meters(float value) => Cached(MetersCache, value, 10f, "0.0", " m");

        public static string Decimal(float value) => Cached(DecimalCache, value, 10f, "0.0", string.Empty);

        public static string Bytes(long value)
        {
            if (value < 0L)
                return NotMeasured;
            return value < IntegerCache.Length ? BytesCache[(int)value] ??= Integer((int)value) + " B" : value.ToString(CultureInfo.InvariantCulture) + " B";
        }

        public static string Position(Vector3 position) =>
            "(" + position.x.ToString("0.0", CultureInfo.InvariantCulture) + ", " + position.y.ToString("0.0", CultureInfo.InvariantCulture) + ", " +
            position.z.ToString("0.0", CultureInfo.InvariantCulture) + ")";

        public static string Name(UnityEngine.Object item)
        {
            if (item == null)
                return None;
            if (Names.TryGetValue(item, out var name))
                return name;
            if (Names.Count >= Limit)
                Names.Clear();
            name = item.name;
            Names[item] = name;
            return name;
        }

        public static void ForgetNames() => Names.Clear();

        public static string TargetsAndLights(int targets, int lights)
        {
            var key = Mathf.Clamp(targets, 0, 9999) * 10000 + Mathf.Clamp(lights, 0, 9999);
            if (Pairs.TryGetValue(key, out var text))
                return text;
            if (Pairs.Count >= Limit)
                Pairs.Clear();
            text = Integer(targets) + (targets == 1 ? " target, " : " targets, ") + Integer(lights) + (lights == 1 ? " light" : " lights");
            Pairs[key] = text;
            return text;
        }

        #endregion


        #region Private and Protected

        private const int Limit = 4096;
        private static readonly string[] PercentCache = new string[101];
        private static readonly string[] IntegerCache = new string[4096];
        private static readonly string[] BytesCache = new string[4096];
        private static readonly Dictionary<int, string> Milli = new(1024);
        private static readonly Dictionary<int, string> SecondsCache = new(1024);
        private static readonly Dictionary<int, string> AgoCache = new(1024);
        private static readonly Dictionary<int, string> MetersCache = new(1024);
        private static readonly Dictionary<int, string> DecimalCache = new(256);
        private static readonly Dictionary<int, string> Pairs = new(64);
        private static readonly Dictionary<UnityEngine.Object, string> Names = new(256);

        private static string Cached(Dictionary<int, string> cache, float value, float scale, string format, string suffix)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return None;
            var key = Mathf.RoundToInt(Mathf.Clamp(value, -1e6f, 1e6f) * scale);
            if (cache.TryGetValue(key, out var text))
                return text;
            if (cache.Count >= Limit)
                cache.Clear();
            text = (key / scale).ToString(format, CultureInfo.InvariantCulture) + suffix;
            cache[key] = text;
            return text;
        }

        #endregion
    }
}
