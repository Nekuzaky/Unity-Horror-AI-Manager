using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    [CreateAssetMenu(menuName = "Horror AI/Archetype", fileName = "Archetype", order = 0)]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/archetypes.md")]
    public sealed class HorrorArchetype : ScriptableObject
    {
        #region Public

        [TextArea(2, 5)] public string description = string.Empty;
        public PerceptionProfile perception;
        public BehaviorProfile behavior;

        public PerceptionProfile Perception => perception != null ? perception : DefaultPerception;

        public BehaviorProfile Behavior => behavior != null ? behavior : DefaultBehavior;

        public static HorrorArchetype Default
        {
            get
            {
                if (_default == null)
                {
                    _default = CreateInstance<HorrorArchetype>();
                    _default.name = "Default";
                    _default.hideFlags = HideFlags.HideAndDontSave;
                }
                return _default;
            }
        }

        #endregion


        #region Private and Protected

        private static HorrorArchetype _default;
        private static PerceptionProfile _defaultPerception;
        private static BehaviorProfile _defaultBehavior;

        private static PerceptionProfile DefaultPerception
        {
            get
            {
                if (_defaultPerception == null)
                {
                    _defaultPerception = CreateInstance<PerceptionProfile>();
                    _defaultPerception.hideFlags = HideFlags.HideAndDontSave;
                }
                return _defaultPerception;
            }
        }

        private static BehaviorProfile DefaultBehavior
        {
            get
            {
                if (_defaultBehavior == null)
                {
                    _defaultBehavior = CreateInstance<BehaviorProfile>();
                    _defaultBehavior.hideFlags = HideFlags.HideAndDontSave;
                }
                return _defaultBehavior;
            }
        }

        #endregion
    }
}
