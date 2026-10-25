using UnityEngine;

namespace Nekuzaky.HorrorAI.Demo
{
    public sealed class DemoHud : MonoBehaviour
    {
        #region Public

        public DemoPlayer player;
        public HorrorEnemy enemy;

        public void Flash() => _flash = 1f;

        #endregion


        #region Private and Protected

        private float _flash;
        private GUIStyle _style;

        private void Update() => _flash = Mathf.Max(0f, _flash - Time.deltaTime * 0.8f);

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            if (_flash > 0f)
            {
                var color = GUI.color;
                GUI.color = new Color(0.7f, 0f, 0f, _flash * 0.7f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = color;
            }
            GUILayout.BeginArea(new Rect(16f, 12f, 520f, 260f));
            GUILayout.Label("<b>Horror AI Manager demo</b>   click to look around, Esc frees the mouse", _style);
            GUILayout.Label("WASD move, Shift run (loud), C crouch (quiet, low), F flashlight", _style);
            if (player != null && player.target != null)
            {
                var lit = player.target.Exposure;
                GUILayout.Label($"You are {(player.target.Crouching ? "crouching" : player.Running ? "running" : "walking")}, " +
                                $"lit {lit:P0}{(player.flashlight != null && player.flashlight.On ? " (flashlight on)" : string.Empty)}. Caught {player.Caught} time(s).", _style);
            }
            if (enemy != null)
                GUILayout.Label($"Enemy: <b>{enemy.State}</b>, suspicion {enemy.Suspicion:P0}, last: {enemy.LastTransition.Explanation}", _style);
            GUILayout.EndArea();
        }

        #endregion
    }
}
