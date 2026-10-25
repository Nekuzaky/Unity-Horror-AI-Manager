using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Nekuzaky.HorrorAI.Editor
{
    internal enum CheckLevel
    {
        Ok,
        Info,
        Warning,
        Error
    }

    internal readonly struct DoctorCheck
    {
        public readonly CheckLevel Level;
        public readonly string Title;
        public readonly string Detail;
        public readonly string Fix;
        public readonly Action Action;

        public DoctorCheck(CheckLevel level, string title, string detail, string fix = null, Action action = null)
        {
            Level = level;
            Title = title;
            Detail = detail;
            Fix = fix;
            Action = action;
        }
    }

    internal static class DoctorChecks
    {
        #region Public

        public static void Run(List<DoctorCheck> checks)
        {
            checks.Clear();
            var enemies = UnityEngine.Object.FindObjectsByType<HorrorEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            Enemies(checks, enemies);
            NavMeshCheck(checks, enemies);
            Targets(checks);
            Lights(checks);
            Archetypes(checks, enemies);
            Agents(checks, enemies);
            Eyes(checks, enemies);
            Routes(checks, enemies);
            Notes(checks, enemies.Length);
        }

        public static int Count(List<DoctorCheck> checks, CheckLevel level)
        {
            var count = 0;
            for (var i = 0; i < checks.Count; i++)
            {
                if (checks[i].Level == level)
                    count++;
            }
            return count;
        }

        public static bool HasNavMesh() => NavMesh.CalculateTriangulation().vertices.Length > 0;

        #endregion


        #region Private and Protected

        private static void Enemies(List<DoctorCheck> checks, HorrorEnemy[] enemies)
        {
            if (enemies.Length == 0)
                checks.Add(new DoctorCheck(CheckLevel.Error, "No Horror Enemy in the open scenes",
                    "Add one: it comes with a NavMesh Agent and the Patroller archetype.", "Add an enemy",
                    () => EditorApplication.ExecuteMenuItem("GameObject/Horror AI/Enemy")));
            else
                checks.Add(new DoctorCheck(CheckLevel.Ok, Plural(enemies.Length, "enemy", "enemies") + " in the open scenes", "Each one is listed in the Enemies tab."));
        }

        private static void NavMeshCheck(List<DoctorCheck> checks, HorrorEnemy[] enemies)
        {
            if (!HasNavMesh())
            {
                checks.Add(new DoctorCheck(CheckLevel.Error, "No NavMesh is baked", "Enemies with a NavMesh Agent cannot walk without one.",
                    "Bake a NavMesh", SceneFixes.BakeNavMesh));
                return;
            }
            var off = 0;
            for (var i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].GetComponent<NavMeshAgent>() != null && !SceneFixes.IsOnNavMesh(enemies[i].transform.position))
                    off++;
            }
            if (off > 0)
                checks.Add(new DoctorCheck(CheckLevel.Error, Plural(off, "enemy stands", "enemies stand") + " off the NavMesh",
                    "They cannot walk. Move them onto the walkable area, or bake again after changing the level.", "Bake again", SceneFixes.BakeNavMesh));
            else
                checks.Add(new DoctorCheck(CheckLevel.Ok, "NavMesh baked", "Every enemy with an agent stands on it."));
        }

        private static void Targets(List<DoctorCheck> checks)
        {
            var count = SceneFixes.CountTargets();
            if (count == 0)
                checks.Add(new DoctorCheck(CheckLevel.Error, "No Horror Target", "Enemies have nobody to see. Put one on the player.",
                    "Add a Horror Target to the player", () => SceneFixes.AddTarget()));
            else if (SceneFixes.CountMisplacedTargets() > 0)
                checks.Add(new DoctorCheck(CheckLevel.Warning, "A Horror Target is not on the player",
                    "It sits on level geometry, the NavMesh or an enemy. Enemies would look for that object instead of the player.",
                    "Move it to the player", () => SceneFixes.MoveTargetsToPlayer()));
            else if (count > HorrorWorld.MaxTargetsPerEnemy)
                checks.Add(new DoctorCheck(CheckLevel.Info, Plural(count, "Horror Target", "Horror Targets"),
                    "Each enemy watches the four first ones only."));
            else
                checks.Add(new DoctorCheck(CheckLevel.Ok, Plural(count, "Horror Target", "Horror Targets"), "Enemies have someone to look for."));
        }

        private static void Lights(List<DoctorCheck> checks)
        {
            var unmarked = SceneFixes.CountUnmarkedLights();
            var unlinked = CountUnlinkedLights();
            if (unmarked > 0)
                checks.Add(new DoctorCheck(CheckLevel.Warning, Plural(unmarked, "light is", "lights are") + " not marked for the AI",
                    "A target under an unmarked lamp counts as standing in the dark.", "Mark every light", SceneFixes.MarkAllLights));
            if (unlinked > 0)
                checks.Add(new DoctorCheck(CheckLevel.Warning, Plural(unlinked, "Horror Light Source is", "Horror Light Sources are") + " not linked to their Light",
                    "Turning the lamp off would not hide the target.", "Link them", LinkLights));
            if (unmarked > 0 || unlinked > 0)
                return;
            var count = SceneFixes.CountLights();
            if (count == 0)
                checks.Add(new DoctorCheck(CheckLevel.Info, "No light is marked for the AI",
                    "Every target counts as standing in the dark (ambient light " + InspectorKit.Percent(HorrorAI.AmbientLight) + ")."));
            else
                checks.Add(new DoctorCheck(CheckLevel.Ok, Plural(count, "light reveals", "lights reveal") + " targets", "Each one is linked to its Light."));
        }

        private static void Archetypes(List<DoctorCheck> checks, HorrorEnemy[] enemies)
        {
            var missing = 0;
            for (var i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].archetype == null)
                    missing++;
            }
            if (missing > 0)
                checks.Add(new DoctorCheck(CheckLevel.Warning, Plural(missing, "enemy has", "enemies have") + " no archetype",
                    "They use the built-in default values.", "Use the Patroller preset", () => ForEach(enemies, e => e.archetype == null, SceneFixes.UsePatroller)));
            else if (enemies.Length > 0)
                checks.Add(new DoctorCheck(CheckLevel.Ok, "Every enemy has an archetype", "See the Archetypes tab for what each one does."));
        }

        private static void Agents(List<DoctorCheck> checks, HorrorEnemy[] enemies)
        {
            var missing = 0;
            for (var i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].GetComponent<NavMeshAgent>() == null)
                    missing++;
            }
            if (missing > 0)
                checks.Add(new DoctorCheck(CheckLevel.Warning, Plural(missing, "enemy has", "enemies have") + " no NavMesh Agent",
                    "Without one an enemy slides in straight lines through walls.", "Add the agents",
                    () => ForEach(enemies, e => e.GetComponent<NavMeshAgent>() == null, SceneFixes.AddAgent)));
            else if (enemies.Length > 0)
                checks.Add(new DoctorCheck(CheckLevel.Ok, "Every enemy has a NavMesh Agent", "They walk around obstacles."));
        }

        private static void Eyes(List<DoctorCheck> checks, HorrorEnemy[] enemies)
        {
            var wrong = 0;
            for (var i = 0; i < enemies.Length; i++)
            {
                var eye = enemies[i].eye;
                if (eye != null && !eye.IsChildOf(enemies[i].transform))
                    wrong++;
            }
            if (wrong > 0)
                checks.Add(new DoctorCheck(CheckLevel.Warning, Plural(wrong, "enemy looks", "enemies look") + " through an eye that is not part of them",
                    "The eye should be the head or a child of the enemy, so sight follows it."));
            else if (enemies.Length > 0)
                checks.Add(new DoctorCheck(CheckLevel.Ok, "Eyes in place", "Sight starts at the eye transform, or at the eye height of the perception profile."));
        }

        private static void Routes(List<DoctorCheck> checks, HorrorEnemy[] enemies)
        {
            var without = 0;
            var problems = 0;
            for (var i = 0; i < enemies.Length; i++)
            {
                var route = enemies[i].patrolRoute;
                if (route == null)
                    without++;
                else if (!RouteIsValid(route))
                    problems++;
            }
            if (problems > 0)
                checks.Add(new DoctorCheck(CheckLevel.Warning, Plural(problems, "patrol route has", "patrol routes have") + " a problem",
                    "Fewer than two waypoints, a missing waypoint, or a waypoint off the NavMesh. Select the route to see which.", "Bake a NavMesh", SceneFixes.BakeNavMesh));
            if (without > 0)
                checks.Add(new DoctorCheck(CheckLevel.Info, Plural(without, "enemy has", "enemies have") + " no patrol route",
                    "They stand guard where they start and come back there after a search.", "Create the routes",
                    () => ForEach(enemies, e => e.patrolRoute == null, e => SceneFixes.CreateRoute(e))));
            if (problems == 0 && without == 0 && enemies.Length > 0)
                checks.Add(new DoctorCheck(CheckLevel.Ok, "Patrol routes are valid", "Every waypoint is set and on the NavMesh."));
        }

        private static void Notes(List<DoctorCheck> checks, int enemies)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var name = PipelineName(pipeline == null ? null : pipeline.GetType().Name);
            checks.Add(new DoctorCheck(CheckLevel.Info, "Rendering: " + name,
                "Light is estimated from the Horror Light Sources, not read from the image, so it works the same with every pipeline."));
            checks.Add(new DoctorCheck(CheckLevel.Info, "Input is yours",
                "The package reads no input. Your player script sets HorrorTarget.Crouching and calls HorrorAI.EmitNoise for footsteps."));
            checks.Add(new DoctorCheck(CheckLevel.Info, "Ray budget: " + HorrorAI.RaycastBudget + " per frame",
                "Perception is spread over frames; with " + Plural(enemies, "enemy", "enemies") + " the budget is " +
                (enemies * HorrorWorld.MaxPointsPerTarget > HorrorAI.RaycastBudget * 4 ? "tight, raise HorrorAI.RaycastBudget." : "plenty.")));
        }

        private static string PipelineName(string type)
        {
            if (type == null)
                return "the built-in pipeline";
            if (type.Contains("Universal"))
                return "URP";
            return type.Contains("HDRender") ? "HDRP" : type;
        }

        private static bool RouteIsValid(PatrolRoute route)
        {
            if (route.Count < 2)
                return false;
            for (var i = 0; i < route.Count; i++)
            {
                if (route.waypoints[i].point == null || !SceneFixes.IsOnNavMesh(route.GetPosition(i)))
                    return false;
            }
            return true;
        }

        private static int CountUnlinkedLights()
        {
            var count = 0;
            foreach (var source in UnityEngine.Object.FindObjectsByType<HorrorLightSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (source.linkedLight == null && source.GetComponent<Light>() != null)
                    count++;
            }
            return count;
        }

        private static void LinkLights()
        {
            foreach (var source in UnityEngine.Object.FindObjectsByType<HorrorLightSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var light = source.GetComponent<Light>();
                if (source.linkedLight != null || light == null)
                    continue;
                Undo.RecordObject(source, "Link Light");
                source.linkedLight = light;
                EditorUtility.SetDirty(source);
            }
        }

        private static void ForEach(HorrorEnemy[] enemies, Func<HorrorEnemy, bool> filter, Action<HorrorEnemy> fix)
        {
            for (var i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && filter(enemies[i]))
                    fix(enemies[i]);
            }
        }

        private static string Plural(int count, string one, string many) => count + " " + (count == 1 ? one : many);

        #endregion
    }
}
