using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nekuzaky.HorrorAI.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Nekuzaky.HorrorAI.Dev
{
    [InitializeOnLoad]
    internal static class Benchmark
    {
        #region Private and Protected

        private const string ActiveKey = "HorrorAI.Benchmark.Active";
        private const string OutputKey = "HorrorAI.Benchmark.Output";
        private const int WarmupFrames = 120;
        private const int MeasuredFrames = 1800;
        private const int EnemyCount = 50;
        private const int LightCount = 8;

        private sealed class Result
        {
            public string Name;
            public double Average;
            public double P95;
            public double Max;
            public double Rays;
            public double Perceptions;
            public long Allocated;
            public string States;
        }

        private static readonly string[] Scenarios = { "50 enemies, one target, lit scene (8 lamps + flashlight)", "50 enemies, one target, dark scene (lights off)" };
        private static readonly List<Result> Results = new();
        private static readonly double[] Costs = new double[MeasuredFrames];
        private static readonly List<HorrorLightSource> Lights = new();

        private static int _scenario;
        private static int _frame;
        private static long _allocated;
        private static long _rays;
        private static long _perceptions;
        private static GameObject _root;
        private static Transform _target;

        #endregion


        #region Main API

        static Benchmark()
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (EditorApplication.isPlaying)
                Begin();
        }

        public static void Run()
        {
            var output = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "Output", "benchmark");
            Directory.CreateDirectory(output);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetString(OutputKey, output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Frame Driver").AddComponent<FrameDriver>();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        #endregion


        #region Driving

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
                Begin();
        }

        private static void Begin()
        {
            FrameDriver.Tick = Step;
            Time.captureFramerate = 60;
            _scenario = 0;
            Setup();
        }

        private static void Step()
        {
            var t = _frame / 60f;
            _target.position = new Vector3(Mathf.Cos(t * 0.25f) * 5f, 0f, Mathf.Sin(t * 0.25f) * 5f);
            if (_frame >= WarmupFrames)
            {
                var stats = HorrorAI.Stats;
                var index = _frame - WarmupFrames;
                Costs[index] = stats.Milliseconds;
                _allocated += stats.AllocatedBytes;
                _rays += stats.Raycasts;
                _perceptions += stats.Perceptions;
            }
            _frame++;
            if (_frame >= WarmupFrames + MeasuredFrames)
                Finish();
        }

        private static void Setup()
        {
            _root = new GameObject("Benchmark Root");
            BuildLevel(_root.transform);
            var surface = _root.GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
            _target = BuildTarget(_root.transform);
            BuildLights(_root.transform, _scenario == 0);
            BuildEnemies(_root.transform);
            _frame = 0;
            _allocated = 0;
            _rays = 0;
            _perceptions = 0;
            Array.Clear(Costs, 0, Costs.Length);
        }

        private static void BuildLevel(Transform root)
        {
            var surface = root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            Box(root, new Vector3(0f, -0.1f, 0f), new Vector3(80f, 0.2f, 80f));
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 0.25f + 0.3f;
                Box(root, new Vector3(Mathf.Cos(angle) * 11f, 1.5f, Mathf.Sin(angle) * 11f), new Vector3(3f, 3f, 0.4f));
            }
        }

        private static Transform BuildTarget(Transform root)
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = "Target";
            target.transform.SetParent(root, false);
            Object.DestroyImmediate(target.GetComponent<CapsuleCollider>());
            var collider = target.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            target.AddComponent<HorrorTarget>();
            var flashlight = new GameObject("Flashlight").AddComponent<HorrorLightSource>();
            flashlight.transform.SetParent(target.transform, false);
            flashlight.transform.localPosition = new Vector3(0f, 1.4f, 0.3f);
            flashlight.shape = HorrorLightShape.Spot;
            flashlight.range = 15f;
            flashlight.revealsHolder = true;
            Lights.Add(flashlight);
            return target.transform;
        }

        private static void BuildLights(Transform root, bool on)
        {
            for (var i = 0; i < LightCount; i++)
            {
                var light = new GameObject("Lamp " + i).AddComponent<HorrorLightSource>();
                light.transform.SetParent(root, false);
                var angle = i * Mathf.PI * 0.25f;
                light.transform.position = new Vector3(Mathf.Cos(angle) * 6f, 2.7f, Mathf.Sin(angle) * 6f);
                light.range = 9f;
                Lights.Add(light);
            }
            foreach (var light in Lights)
                light.SetOn(on);
        }

        private static void BuildEnemies(Transform root)
        {
            var archetype = SceneFixes.Patroller;
            for (var i = 0; i < EnemyCount; i++)
            {
                var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = "Enemy " + i;
                enemy.transform.SetParent(root, false);
                var angle = i * Mathf.PI * 2f / EnemyCount;
                var radius = 8f + i % 5 * 3f;
                enemy.transform.position = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
                enemy.transform.rotation = Quaternion.LookRotation(-enemy.transform.position.normalized + Vector3.right * (i % 3 - 1) * 0.6f);
                var agent = enemy.AddComponent<NavMeshAgent>();
                agent.baseOffset = 1f;
                var horror = enemy.AddComponent<HorrorEnemy>();
                horror.archetype = archetype;
                horror.seed = i + 1;
            }
        }

        private static void Box(Transform root, Vector3 position, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(root, false);
            box.transform.position = position;
            box.transform.localScale = size;
        }

        private static void Finish()
        {
            var sorted = (double[])Costs.Clone();
            Array.Sort(sorted);
            var total = 0d;
            foreach (var cost in sorted)
                total += cost;
            Results.Add(new Result
            {
                Name = Scenarios[_scenario],
                Average = total / sorted.Length,
                P95 = sorted[(int)(sorted.Length * 0.95)],
                Max = sorted[sorted.Length - 1],
                Rays = _rays / (double)MeasuredFrames,
                Perceptions = _perceptions / (double)MeasuredFrames,
                Allocated = _allocated,
                States = CountStates()
            });
            Object.Destroy(_root);
            Lights.Clear();
            _scenario++;
            if (_scenario < Scenarios.Length)
            {
                Setup();
                return;
            }
            Write();
            SessionState.SetBool(ActiveKey, false);
            FrameDriver.Tick = null;
            EditorApplication.Exit(0);
        }

        private static string CountStates()
        {
            var counts = new int[6];
            var enemies = HorrorAI.Enemies;
            for (var i = 0; i < enemies.Count; i++)
                counts[(int)enemies[i].State]++;
            var text = new StringBuilder();
            for (var s = 0; s < counts.Length; s++)
            {
                if (counts[s] > 0)
                    text.Append(text.Length > 0 ? ", " : string.Empty).Append(counts[s]).Append(' ').Append((HorrorState)s);
            }
            return text.ToString();
        }

        private static void Write()
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var text = new StringBuilder();
            text.AppendLine("| Scenario | Horror AI per frame (avg / p95 / max) | Raycasts per frame | Perceptions per frame | GC per frame | States at the end |");
            text.AppendLine("|---|---:|---:|---:|---:|---|");
            foreach (var r in Results)
            {
                text.AppendLine(string.Format(culture, "| {0} | {1:0.000} / {2:0.000} / {3:0.000} ms | {4:0.0} | {5:0.0} | {6} B | {7} |",
                    r.Name, r.Average, r.P95, r.Max, r.Rays, r.Perceptions, r.Allocated / MeasuredFrames, r.States));
            }
            text.AppendLine();
            text.AppendLine(string.Format(culture, "Measured on {0}, {1} logical cores, Unity {2}, Editor Play Mode in batch mode, {3:N0} frames per scenario at 60 simulated fps after {4} warm-up frames. Raycast budget {5}.",
                SystemInfo.processorType.Trim(), SystemInfo.processorCount, Application.unityVersion, MeasuredFrames, WarmupFrames, HorrorAI.RaycastBudget));
            var output = SessionState.GetString(OutputKey, Path.Combine("Tools", "Output", "benchmark"));
            File.WriteAllText(Path.Combine(output, "benchmark.md"), text.ToString());
            Debug.Log("[HorrorAI] Benchmark written to " + output + "\n" + text);
        }

        #endregion
    }
}
