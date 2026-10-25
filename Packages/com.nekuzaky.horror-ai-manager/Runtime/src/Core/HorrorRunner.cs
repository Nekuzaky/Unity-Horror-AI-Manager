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
            if (Application.isPlaying)
                DontDestroyOnLoad(host);
            _instance = host.AddComponent<HorrorRunner>();
        }

        public static void DestroyAll()
        {
            _instance = null;
            var runners = Resources.FindObjectsOfTypeAll<HorrorRunner>();
            for (var i = 0; i < runners.Length; i++)
            {
                if (runners[i] != null)
                    DestroyImmediate(runners[i].gameObject);
            }
        }

        internal static HorrorRunner Current => _instance;

        internal void Run(float deltaTime)
        {
            if (_instance != this)
            {
                Retire();
                return;
            }
            HorrorAI.World.Tick(deltaTime);
        }

        #endregion


        #region Private and Protected

        private static HorrorRunner _instance;

        private void Update() => Run(UnityEngine.Time.deltaTime);

        private void Retire()
        {
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        #endregion
    }
}
