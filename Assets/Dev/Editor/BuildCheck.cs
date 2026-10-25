using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Dev
{
    internal static class BuildCheck
    {
        #region Main API

        private const string Scene = "Assets/Dev/Demo/Horror AI Demo.unity";

        public static void Run()
        {
            var output = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "Output", "build");
            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(output, "HorrorAIDemo.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { "HORROR_AI_BUILD_PROBE" }
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[BuildCheck] {summary.result}: {summary.totalSize / 1048576d:0.0} MB in {summary.totalTime.TotalSeconds:0} s, {summary.totalErrors} error(s), {summary.totalWarnings} warning(s)");
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        #endregion
    }
}
