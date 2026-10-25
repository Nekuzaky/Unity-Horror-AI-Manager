using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(100)]
    internal sealed class HorrorRunner : MonoBehaviour
    {
        #region Public

        public static void Ensure()
        {
            if (_instance != null)
                return;
            var host = new GameObject("Horror AI") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<HorrorRunner>();
        }

        public static void Reset() => _instance = null;

        #endregion


        #region Private and Protected

        private static HorrorRunner _instance;

        private void Update() => HorrorAI.World.Tick(UnityEngine.Time.deltaTime);

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        #endregion
    }
}
