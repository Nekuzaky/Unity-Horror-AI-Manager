using System;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Dev
{
    [DefaultExecutionOrder(1000)]
    public sealed class FrameDriver : MonoBehaviour
    {
        public static Action Tick;

        private void Update() => Tick?.Invoke();
    }
}