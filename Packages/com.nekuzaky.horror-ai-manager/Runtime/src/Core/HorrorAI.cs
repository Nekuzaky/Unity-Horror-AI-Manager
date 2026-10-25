using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.HorrorAI
{
    public static class HorrorAI
    {
        #region Public

        public static event Action<HorrorNoise> NoiseEmitted;

        public static event Action<HorrorEnemy, HorrorTransition> StateChanged;

        public static event Action<HorrorEnemy> EnemyAdded;

        public static event Action<HorrorEnemy> EnemyRemoved;

        public static IReadOnlyList<HorrorEnemy> Enemies => World.Enemies;

        public static IReadOnlyList<HorrorTarget> Targets => World.Targets;

        public static IReadOnlyList<HorrorLightSource> Lights => World.Lights;

        public static HorrorAIStats Stats => World.Stats;

        public static float Time => World.Now;

        public static int RaycastBudget
        {
            get => World.RaycastBudget;
            set => World.RaycastBudget = value;
        }

        public static float AmbientLight
        {
            get => World.AmbientLight;
            set => World.AmbientLight = value;
        }

        public static void EmitNoise(Vector3 position, float loudness, GameObject source = null)
        {
            World.EmitNoise(position, loudness, source);
            NoiseEmitted?.Invoke(new HorrorNoise(position, loudness, source, World.Now));
        }

        public static float GetExposure(HorrorTarget target) => World.ExposureOf(target);

        public static int CopyRecentNoises(HorrorNoise[] buffer)
        {
            if (buffer == null)
                return 0;
            var world = World;
            var copied = 0;
            var last = world.NoiseSequence - 1;
            for (var i = 0; i < buffer.Length && i < HorrorWorld.NoiseCapacity; i++)
            {
                if (!world.TryGetNoise(last - i, out var noise))
                    break;
                buffer[copied++] = noise;
            }
            return copied;
        }

        #endregion


        #region Private and Protected

        private static HorrorWorld _world;

        internal static HorrorWorld World
        {
            get
            {
                _world ??= new HorrorWorld();
                return _world;
            }
        }

        internal static HorrorWorld ResetWorld()
        {
            _world = new HorrorWorld();
            return _world;
        }

        internal static void RaiseStateChanged(HorrorEnemy enemy, HorrorTransition transition) => StateChanged?.Invoke(enemy, transition);

        internal static void RaiseEnemyAdded(HorrorEnemy enemy) => EnemyAdded?.Invoke(enemy);

        internal static void RaiseEnemyRemoved(HorrorEnemy enemy) => EnemyRemoved?.Invoke(enemy);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetSession();

        internal static void ResetSession()
        {
            _world = new HorrorWorld();
            NoiseEmitted = null;
            StateChanged = null;
            EnemyAdded = null;
            EnemyRemoved = null;
            HorrorRunner.DestroyAll();
        }

        internal static void EnsureRunner()
        {
            if (Application.isPlaying)
                HorrorRunner.Ensure();
        }

        #endregion
    }
}
