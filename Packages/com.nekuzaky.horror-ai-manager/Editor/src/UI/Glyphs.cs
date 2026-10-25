using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal static class Glyphs
    {
        #region Public

        public const string Logo = "horror-eye";
        public const string Overview = "speedometer2";
        public const string Enemies = "crosshair";
        public const string Noises = "broadcast";
        public const string Setup = "clipboard";
        public const string Archetypes = "layers-fill";
        public const string Pause = "pause-fill";
        public const string Resume = "play-fill";
        public const string Follow = "pin-angle-fill";
        public const string Docs = "book";
        public const string GitHub = "github";
        public const string Ping = "box-arrow-up-right";
        public const string Add = "plus-lg";
        public const string Fix = "magic";
        public const string Refresh = "arrow-repeat";
        public const string Folder = "folder2-open";
        public const string Cost = "cpu";
        public const string Rays = "activity";
        public const string Perceptions = "eye-fill";
        public const string Garbage = "memory";
        public const string Suspicion = "graph-up";
        public const string Hearing = "ear-fill";
        public const string Memory = "clock-history";
        public const string Light = "light";
        public const string Target = "bullseye";
        public const string Time = "stopwatch";
        public const string Ok = "check-circle-fill";
        public const string Info = "info-circle-fill";
        public const string Warning = "exclamation-triangle-fill";
        public const string Error = "x-octagon-fill";

        public static readonly string[] All =
        {
            Logo, Overview, Enemies, Noises, Setup, Archetypes, Pause, Resume, Follow, Docs, GitHub, Ping, Add, Fix, Refresh, Folder,
            Cost, Rays, Perceptions, Garbage, Suspicion, Hearing, Memory, Light, Target, Time, Ok, Info, Warning, Error,
            "arrow-repeat", "exclamation-triangle-fill", "search", "map-fill", "lightning-fill", "question-circle", "eye-fill",
            "diagram-3-fill", "signpost-split-fill", "soundwave"
        };

        public static readonly string[] Tiles =
        {
            "HorrorAI", "HorrorEnemy", "HorrorTarget", "HorrorLightSource", "NoiseEmitter", "PatrolRoute", "HorrorArchetype",
            "PerceptionProfile", "BehaviorProfile", "ResonanceNoiseMap", "ResonanceNoiseBridge", "HorrorHindsightProbe"
        };

        public static string Of(HorrorState state)
        {
            switch (state)
            {
                case HorrorState.Alert: return "exclamation-triangle-fill";
                case HorrorState.Investigate: return "search";
                case HorrorState.Search: return "map-fill";
                case HorrorState.Chase: return "lightning-fill";
                case HorrorState.LostTrack: return "question-circle";
                default: return "arrow-repeat";
            }
        }

        public static StyleSheet StyleSheet => AssetDatabase.LoadAssetAtPath<StyleSheet>(Root + "/Dashboard.uss");

        public static Texture2D Tile(string name)
        {
            if (TileCache.TryGetValue(name, out var cached) && cached != null)
                return cached;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TileFolder + "/" + name + ".png");
            TileCache[name] = texture;
            return texture;
        }

        public static VectorImage Get(string name)
        {
            if (Cache.TryGetValue(name, out var cached) && cached != null)
                return cached;
            var image = AssetDatabase.LoadAssetAtPath<VectorImage>(GlyphFolder + "/" + name + ".svg");
            Cache[name] = image;
            return image;
        }

        public static VisualElement Icon(string name, params string[] classes)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("ha-icon");
            icon.style.backgroundImage = new StyleBackground(Get(name));
            for (var i = 0; i < classes.Length; i++)
                icon.AddToClassList(classes[i]);
            return icon;
        }

        public static void Set(VisualElement icon, string name) => icon.style.backgroundImage = new StyleBackground(Get(name));

        public static Image TileImage(string name, string className)
        {
            var image = new Image { image = Tile(name), pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
            image.AddToClassList(className);
            return image;
        }

        #endregion


        #region Private and Protected

        private const string Package = "Packages/com.nekuzaky.horror-ai-manager/Editor";
        private const string Root = Package + "/UI";
        private const string GlyphFolder = Root + "/Icons";
        private const string TileFolder = Package + "/Icons";

        private static readonly Dictionary<string, VectorImage> Cache = new();
        private static readonly Dictionary<string, Texture2D> TileCache = new();

        #endregion
    }
}
