using UnityEditor;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Editor
{
    internal static class NoisePing
    {
        #region Public

        public const double Duration = 2d;

        public static bool Active => _until > EditorApplication.timeSinceStartup;

        public static void Show(Vector3 position, float loudness)
        {
            _position = position;
            _radius = Mathf.Clamp(loudness * 0.25f, 0.5f, 4f);
            _until = EditorApplication.timeSinceStartup + Duration;
            if (!_hooked)
            {
                _hooked = true;
                SceneView.duringSceneGui += Draw;
            }
            var view = SceneView.lastActiveSceneView;
            if (view == null)
                return;
            view.LookAt(position, view.rotation, Mathf.Max(6f, loudness));
            view.Repaint();
        }

        #endregion


        #region Private and Protected

        private static Vector3 _position;
        private static float _radius;
        private static double _until;
        private static bool _hooked;

        private static void Draw(SceneView view)
        {
            var left = _until - EditorApplication.timeSinceStartup;
            if (left <= 0d)
            {
                SceneView.duringSceneGui -= Draw;
                _hooked = false;
                return;
            }
            var t = (float)(1d - left / Duration);
            var color = new Color(1f, 0.6f, 0.15f, 1f - t);
            Handles.color = color;
            for (var i = 0; i < 3; i++)
                Handles.DrawWireDisc(_position, Vector3.up, _radius * (0.4f + i * 0.5f + t * 1.5f), 2f);
            Handles.DrawSolidDisc(_position, Vector3.up, 0.12f);
            view.Repaint();
        }

        #endregion
    }
}
