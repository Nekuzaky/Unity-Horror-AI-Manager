using System.IO;
using System.Reflection;
using Nekuzaky.HorrorAI.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Dev
{
    internal static class CaptureKit
    {
        #region Public

        public static string Images => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "images"));

        public static string Frames => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "Output", "media"));

        public static bool Save(EditorWindow window, string name)
        {
            var texture = window != null ? Grab(window) : null;
            if (texture == null)
            {
                Debug.LogWarning("[Capture] could not capture " + name);
                return false;
            }
            Directory.CreateDirectory(Images);
            File.WriteAllBytes(Path.Combine(Images, name + ".png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Debug.Log("Captured " + name);
            return true;
        }

        public static void SaveFrame(EditorWindow window, string folder, int index)
        {
            var texture = window != null ? Grab(window) : null;
            if (texture == null)
                return;
            var directory = Path.Combine(Frames, folder);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "frame_" + index.ToString("0000") + ".png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        public static void ResetFrames(string folder)
        {
            var directory = Path.Combine(Frames, folder);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);
        }

        public static Texture2D Grab(EditorWindow window)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var parent = typeof(EditorWindow).GetField("m_Parent", flags)?.GetValue(window);
            var grab = parent?.GetType().GetMethod("GrabPixels", flags);
            if (grab == null)
                return null;
            var scale = EditorGUIUtility.pixelsPerPoint;
            var width = Mathf.RoundToInt(window.position.width * scale);
            var height = Mathf.RoundToInt(window.position.height * scale);
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            typeof(EditorWindow).GetMethod("RepaintImmediately", flags)?.Invoke(window, null);
            grab.Invoke(parent, new object[] { target, new Rect(0f, 0f, width, height) });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            Flip(texture);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return texture;
        }

        public static EditorWindow FindWindow(string typeName)
        {
            var type = typeof(UnityEditor.Editor).Assembly.GetType(typeName);
            return type != null ? EditorWindow.GetWindow(type) : null;
        }

        #endregion


        #region Private and Protected

        private static void Flip(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();
            var flipped = new Color32[pixels.Length];
            for (var y = 0; y < height; y++)
                System.Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
            texture.SetPixels32(flipped);
            texture.Apply();
        }

        #endregion
    }

    internal sealed class InspectorPreviewWindow : EditorWindow
    {
        #region Public

        public static InspectorPreviewWindow Show(Rect position, string title, params Object[] targets)
        {
            var window = CreateInstance<InspectorPreviewWindow>();
            window.titleContent = new GUIContent(title);
            window.ShowUtility();
            window.position = position;
            var root = window.rootVisualElement;
            root.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.78f, 0.78f, 0.78f);
            var scroll = new ScrollView();
            root.Add(scroll);
            foreach (var target in targets)
            {
                if (target == null)
                    continue;
                scroll.Add(Header(target));
                scroll.Add(new InspectorElement(target) { style = { marginLeft = 4f, marginRight = 4f } });
            }
            return window;
        }

        #endregion


        #region Private and Protected

        private static VisualElement Header(Object target)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 10f, marginLeft = 8f, marginBottom = 2f } };
            var content = EditorGUIUtility.ObjectContent(target, target.GetType());
            row.Add(new Image { image = content.image, style = { width = 20f, height = 20f, marginRight = 6f } });
            var name = target is Component component ? component.gameObject.name + "  ·  " + ObjectNames.NicifyVariableName(target.GetType().Name) : target.name;
            row.Add(new Label(name) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13 } });
            return row;
        }

        #endregion
    }

    internal sealed class IconSheetWindow : EditorWindow
    {
        #region Public

        public static IconSheetWindow Show(Rect position)
        {
            var window = CreateInstance<IconSheetWindow>();
            window.titleContent = new GUIContent("Horror AI icons");
            window.ShowUtility();
            window.position = position;
            var root = window.rootVisualElement;
            root.style.backgroundColor = new Color(0.16f, 0.16f, 0.17f);
            root.style.paddingLeft = 16f;
            root.style.paddingTop = 12f;
            var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            root.Add(grid);
            foreach (var name in Glyphs.Tiles)
                grid.Add(Cell(name));
            return window;
        }

        #endregion


        #region Private and Protected

        private static VisualElement Cell(string name)
        {
            var cell = new VisualElement { style = { width = 150f, height = 104f, alignItems = Align.Center, marginBottom = 8f } };
            cell.Add(new Image { image = Glyphs.Tile(name), style = { width = 56f, height = 56f, marginBottom = 6f } });
            var small = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            small.Add(new Image { image = Glyphs.Tile(name), style = { width = 16f, height = 16f, marginRight = 4f } });
            small.Add(new Label(ObjectNames.NicifyVariableName(name)) { style = { color = new Color(0.86f, 0.86f, 0.88f), fontSize = 11 } });
            cell.Add(small);
            return cell;
        }

        #endregion
    }
}
