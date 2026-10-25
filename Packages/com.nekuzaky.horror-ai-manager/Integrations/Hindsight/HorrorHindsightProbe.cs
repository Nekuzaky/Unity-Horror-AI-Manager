using System;
using Nekuzaky.Hindsight;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Integrations
{
    [AddComponentMenu("Horror AI/Hindsight Probe")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/integrations.md")]
    public sealed class HorrorHindsightProbe : MonoBehaviour
    {
        #region Public

        [NonSerialized, HindsightTrack("AI state")] public HorrorState state;
        [NonSerialized, HindsightTrack("Suspicion")] public float suspicion;
        [NonSerialized, HindsightTrack("Sees target")] public bool sees;
        [NonSerialized, HindsightTrack("Target lit")] public float exposure;
        [NonSerialized, HindsightTrack("Memory certainty")] public float certainty;
        [NonSerialized, HindsightTrack("Last known position")] public Vector3 lastKnownPosition;

        public void Sample()
        {
            if (_enemy == null)
                _enemy = GetComponent<HorrorEnemy>();
            if (_enemy == null)
                return;
            var perception = _enemy.Perception;
            var memory = _enemy.Memory;
            state = _enemy.State;
            suspicion = _enemy.Suspicion;
            sees = perception.Sees;
            exposure = perception.Exposure;
            certainty = memory.Certainty;
            lastKnownPosition = memory.Position;
        }

        #endregion


        #region Private and Protected

        private HorrorEnemy _enemy;

        private void LateUpdate() => Sample();

        #endregion
    }
}
