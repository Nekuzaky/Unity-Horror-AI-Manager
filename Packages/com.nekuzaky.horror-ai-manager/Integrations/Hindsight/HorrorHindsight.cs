using System.Collections.Generic;
using UnityEngine;
using HindsightApi = Nekuzaky.Hindsight.Hindsight;

namespace Nekuzaky.HorrorAI.Integrations
{
    public static class HorrorHindsight
    {
        #region Public

        public static bool Bookmarks { get; set; } = true;

        public static bool TrackValues { get; set; } = true;

        public static string Label(HorrorEnemy enemy, HorrorTransition transition)
        {
            if (enemy == null)
                return Build("Enemy", transition);
            if (!Labels.TryGetValue(enemy, out var labels))
            {
                labels = new LabelSet(enemy.name);
                Labels.Add(enemy, labels);
            }
            var key = Key(transition);
            if (!labels.Texts.TryGetValue(key, out var text))
            {
                text = Build(labels.Name, transition);
                labels.Texts.Add(key, text);
            }
            return text;
        }

        #endregion


        #region Private and Protected

        private const int States = 6;

        private sealed class LabelSet
        {
            public readonly string Name;
            public readonly Dictionary<int, string> Texts = new(48);

            public LabelSet(string name)
            {
                Name = name;
            }
        }

        private static readonly HorrorTransition[] Usual =
        {
            new(HorrorState.Patrol, HorrorState.Alert, TransitionReason.SuspiciousSight, 0f),
            new(HorrorState.Patrol, HorrorState.Alert, TransitionReason.HeardNoise, 0f),
            new(HorrorState.Patrol, HorrorState.Chase, TransitionReason.Spotted, 0f),
            new(HorrorState.Alert, HorrorState.Chase, TransitionReason.Spotted, 0f),
            new(HorrorState.Alert, HorrorState.Investigate, TransitionReason.AlertOver, 0f),
            new(HorrorState.Alert, HorrorState.Patrol, TransitionReason.CalmedDown, 0f),
            new(HorrorState.Investigate, HorrorState.Chase, TransitionReason.Spotted, 0f),
            new(HorrorState.Investigate, HorrorState.Search, TransitionReason.ReachedLastKnownPosition, 0f),
            new(HorrorState.Investigate, HorrorState.Search, TransitionReason.CouldNotReach, 0f),
            new(HorrorState.Investigate, HorrorState.Search, TransitionReason.InvestigationTimedOut, 0f),
            new(HorrorState.Search, HorrorState.Chase, TransitionReason.Spotted, 0f),
            new(HorrorState.Search, HorrorState.Investigate, TransitionReason.HeardNoise, 0f),
            new(HorrorState.Search, HorrorState.Investigate, TransitionReason.SuspiciousSight, 0f),
            new(HorrorState.Search, HorrorState.Patrol, TransitionReason.GaveUp, 0f),
            new(HorrorState.Chase, HorrorState.LostTrack, TransitionReason.LostSight, 0f),
            new(HorrorState.LostTrack, HorrorState.Chase, TransitionReason.Reacquired, 0f),
            new(HorrorState.LostTrack, HorrorState.Search, TransitionReason.ReachedLastKnownPosition, 0f),
            new(HorrorState.LostTrack, HorrorState.Search, TransitionReason.CouldNotReach, 0f),
            new(HorrorState.LostTrack, HorrorState.Search, TransitionReason.LostTrackOver, 0f)
        };

        private static readonly Dictionary<HorrorEnemy, LabelSet> Labels = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Install()
        {
            Labels.Clear();
            HorrorAI.StateChanged -= OnStateChanged;
            HorrorAI.StateChanged += OnStateChanged;
            HorrorAI.EnemyAdded -= OnEnemyAdded;
            HorrorAI.EnemyAdded += OnEnemyAdded;
            HorrorAI.EnemyRemoved -= OnEnemyRemoved;
            HorrorAI.EnemyRemoved += OnEnemyRemoved;
        }

        internal static void OnStateChanged(HorrorEnemy enemy, HorrorTransition transition)
        {
            if (Bookmarks)
                HindsightApi.Bookmark(Label(enemy, transition));
        }

        internal static void OnEnemyAdded(HorrorEnemy enemy)
        {
            if (enemy == null)
                return;
            if (Bookmarks)
                WarmLabels(enemy);
            if (!TrackValues || !Application.isEditor || enemy.GetComponent<HorrorHindsightProbe>() != null)
                return;
            enemy.gameObject.AddComponent<HorrorHindsightProbe>();
        }

        internal static void OnEnemyRemoved(HorrorEnemy enemy)
        {
            if (!ReferenceEquals(enemy, null))
                Labels.Remove(enemy);
        }

        private static void WarmLabels(HorrorEnemy enemy)
        {
            for (var i = 0; i < Usual.Length; i++)
                Label(enemy, Usual[i]);
            for (var from = 1; from < States; from++)
                Label(enemy, new HorrorTransition((HorrorState)from, HorrorState.Patrol, TransitionReason.Disabled, 0f));
        }

        private static int Key(HorrorTransition transition) =>
            ((int)transition.From * States + (int)transition.To) * 64 + (int)transition.Reason;

        private static string Build(string name, HorrorTransition transition) =>
            name + ": " + transition.From + " > " + transition.To + " (" + transition.Explanation + ")";

        #endregion
    }
}
