using UnityEngine;
using HindsightApi = Nekuzaky.Hindsight.Hindsight;

namespace Nekuzaky.HorrorAI.Integrations
{
    public static class HorrorHindsight
    {
        #region Public

        public static bool Bookmarks { get; set; } = true;

        public static bool TrackValues { get; set; } = true;

        public static string Label(HorrorEnemy enemy, HorrorTransition transition) =>
            (enemy != null ? enemy.name : "Enemy") + ": " + transition.From + " > " + transition.To + " (" + transition.Explanation + ")";

        #endregion


        #region Private and Protected

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            HorrorAI.StateChanged -= OnStateChanged;
            HorrorAI.StateChanged += OnStateChanged;
            HorrorAI.EnemyAdded -= OnEnemyAdded;
            HorrorAI.EnemyAdded += OnEnemyAdded;
        }

        internal static void OnStateChanged(HorrorEnemy enemy, HorrorTransition transition)
        {
            if (Bookmarks)
                HindsightApi.Bookmark(Label(enemy, transition));
        }

        internal static void OnEnemyAdded(HorrorEnemy enemy)
        {
            if (!TrackValues || !Application.isEditor || enemy == null || enemy.GetComponent<HorrorHindsightProbe>() != null)
                return;
            enemy.gameObject.AddComponent<HorrorHindsightProbe>();
        }

        #endregion
    }
}
