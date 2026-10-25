using System.Diagnostics;

namespace Nekuzaky.HorrorAI
{
    internal interface IHorrorProbeListener
    {
        void Ticked(HorrorWorld world);

        void Changed(HorrorEnemy enemy, HorrorTransition transition);

        void Heard(HorrorEnemy enemy, int noise, float level, int walls, bool heard);
    }

    internal static class HorrorProbe
    {
        #region Public

        public static IHorrorProbeListener Listener { get; set; }

        [Conditional("UNITY_EDITOR")]
        public static void Ticked(HorrorWorld world) => Listener?.Ticked(world);

        [Conditional("UNITY_EDITOR")]
        public static void Changed(HorrorEnemy enemy, HorrorTransition transition) => Listener?.Changed(enemy, transition);

        [Conditional("UNITY_EDITOR")]
        public static void Heard(HorrorEnemy enemy, int noise, float level, int walls, bool heard) => Listener?.Heard(enemy, noise, level, walls, heard);

        #endregion
    }
}
