using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Nekuzaky.HorrorAI.Editor
{
    internal static class SceneFixes
    {
        #region Public

        public const string PackagePath = "Packages/com.nekuzaky.horror-ai-manager";
        public const string PatrollerPath = PackagePath + "/Runtime/Presets/Patroller.asset";

        public static HorrorArchetype Patroller => AssetDatabase.LoadAssetAtPath<HorrorArchetype>(PatrollerPath);

        public static bool IsOnNavMesh(Vector3 position, float maxDistance = 2f) =>
            NavMesh.SamplePosition(position, out _, maxDistance, NavMesh.AllAreas);

        public static int CountTargets() => Object.FindObjectsByType<HorrorTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

        public static int CountEnemies() => Object.FindObjectsByType<HorrorEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

        public static int CountLights() => Object.FindObjectsByType<HorrorLightSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

        public static int CountUnmarkedLights()
        {
            var count = 0;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional && light.GetComponent<HorrorLightSource>() == null)
                    count++;
            }
            return count;
        }

        public static void MarkAllLights()
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional || light.GetComponent<HorrorLightSource>() != null)
                    continue;
                var source = Undo.AddComponent<HorrorLightSource>(light.gameObject);
                source.linkedLight = light;
                source.range = light.range;
                source.spotAngle = light.spotAngle;
                source.shape = light.type == LightType.Spot ? HorrorLightShape.Spot : HorrorLightShape.Point;
            }
        }

        public static void BakeNavMesh()
        {
            var surface = Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                var host = new GameObject("NavMesh");
                Undo.RegisterCreatedObjectUndo(host, "Add NavMesh Surface");
                surface = host.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
            }
            NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { surface });
            Selection.activeObject = surface;
        }

        public static HorrorTarget AddTarget()
        {
            var host = PlayerCandidate();
            if (host == null)
            {
                host = new GameObject("Player");
                Undo.RegisterCreatedObjectUndo(host, "Add Horror Target");
            }
            var target = host.GetComponent<HorrorTarget>();
            if (target == null)
                target = Undo.AddComponent<HorrorTarget>(host);
            Selection.activeObject = host;
            EditorGUIUtility.PingObject(host);
            return target;
        }

        public static PatrolRoute CreateRoute(HorrorEnemy enemy)
        {
            var host = new GameObject(enemy.name + " Route");
            Undo.RegisterCreatedObjectUndo(host, "Create Patrol Route");
            var route = host.AddComponent<PatrolRoute>();
            var start = enemy.transform.position;
            route.transform.position = start;
            route.AddWaypoint(start, 1.5f);
            route.AddWaypoint(start + enemy.transform.forward * 6f, 1.5f);
            Undo.RecordObject(enemy, "Assign Patrol Route");
            enemy.patrolRoute = route;
            EditorUtility.SetDirty(enemy);
            return route;
        }

        public static void UsePatroller(HorrorEnemy enemy)
        {
            Undo.RecordObject(enemy, "Use Patroller preset");
            enemy.archetype = Patroller;
            EditorUtility.SetDirty(enemy);
        }

        public static void AddAgent(HorrorEnemy enemy)
        {
            var agent = Undo.AddComponent<NavMeshAgent>(enemy.gameObject);
            agent.speed = enemy.BehaviorProfile.patrolSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
        }

        #endregion


        #region Menus

        [MenuItem("GameObject/Horror AI/Enemy", false, 10)]
        private static void CreateEnemy(MenuCommand command)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Horror Enemy";
            enemy.transform.position = SpawnPoint(command) + Vector3.up;
            GameObjectUtility.SetParentAndAlign(enemy, command.context as GameObject);
            var agent = enemy.AddComponent<NavMeshAgent>();
            agent.baseOffset = 1f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            enemy.AddComponent<HorrorEnemy>().archetype = Patroller;
            Undo.RegisterCreatedObjectUndo(enemy, "Create Horror Enemy");
            Selection.activeObject = enemy;
        }

        [MenuItem("GameObject/Horror AI/Patrol Route", false, 11)]
        private static void CreateRouteMenu(MenuCommand command)
        {
            var host = new GameObject("Patrol Route");
            GameObjectUtility.SetParentAndAlign(host, command.context as GameObject);
            var origin = SpawnPoint(command);
            host.transform.position = origin;
            var route = host.AddComponent<PatrolRoute>();
            route.AddWaypoint(origin, 1.5f);
            route.AddWaypoint(origin + Vector3.forward * 6f, 1.5f);
            Undo.RegisterCreatedObjectUndo(host, "Create Patrol Route");
            Selection.activeObject = host;
        }

        [MenuItem("GameObject/Horror AI/Mark Every Light For The AI", false, 30)]
        private static void MarkLightsMenu() => MarkAllLights();

        #endregion


        #region Private and Protected

        private static GameObject PlayerCandidate()
        {
            var tagged = GameObject.FindWithTag("Player");
            if (tagged != null)
                return tagged;
            var camera = Camera.main;
            if (camera != null)
                return camera.transform.root.gameObject;
            return Selection.activeGameObject;
        }

        private static Vector3 SpawnPoint(MenuCommand command)
        {
            if (command.context is GameObject parent)
                return parent.transform.position;
            var view = SceneView.lastActiveSceneView;
            return view != null ? view.pivot : Vector3.zero;
        }

        #endregion
    }
}
