using UnityEngine;

namespace Nekuzaky.HorrorAI.Demo
{
    [RequireComponent(typeof(HorrorEnemy))]
    public sealed class DemoEnemyLook : MonoBehaviour
    {
        #region Public

        public Renderer body;
        public Light eyeLight;

        public void Show(HorrorState state)
        {
            var color = ColorOf(state);
            if (body != null)
            {
                _block ??= new MaterialPropertyBlock();
                body.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, color);
                _block.SetColor(LegacyColor, color);
                body.SetPropertyBlock(_block);
            }
            if (eyeLight != null)
                eyeLight.color = color;
        }

        public static Color ColorOf(HorrorState state)
        {
            switch (state)
            {
                case HorrorState.Alert: return new Color(1f, 0.85f, 0.2f);
                case HorrorState.Investigate: return new Color(1f, 0.55f, 0.1f);
                case HorrorState.Search: return new Color(0.7f, 0.4f, 1f);
                case HorrorState.Chase: return new Color(1f, 0.1f, 0.1f);
                case HorrorState.LostTrack: return new Color(1f, 0.3f, 0.6f);
                default: return new Color(0.3f, 0.75f, 0.45f);
            }
        }

        #endregion


        #region Private and Protected

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColor = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _block;

        private void Start() => Show(GetComponent<HorrorEnemy>().State);

        #endregion
    }
}
