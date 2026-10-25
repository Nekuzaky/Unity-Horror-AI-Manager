using Nekuzaky.HorrorAI.Demo;
using Nekuzaky.HorrorAI.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Nekuzaky.HorrorAI.Dev
{
    internal static class DemoSceneBuilder
    {
        #region Main API

        private const string Folder = "Assets/Dev/Demo";
        private const string ScenePath = Folder + "/Horror AI Demo.unity";

        [MenuItem("Tools/Horror AI Dev/Build Demo Scene")]
        public static void Build()
        {
            PresetBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting();
            var level = new GameObject("Level").transform;
            BuildLevel(level);
            BakeNavMesh(level);
            BuildLamps(level);
            var route = BuildRoute();
            var player = BuildPlayer();
            var enemy = BuildEnemy(route);
            BuildProps();
            var hud = new GameObject("HUD").AddComponent<DemoHud>();
            hud.player = player;
            hud.enemy = enemy;
            UnityEventTools.AddPersistentListener(enemy.onCaught, new UnityAction(player.Respawn));
            UnityEventTools.AddPersistentListener(enemy.onCaught, new UnityAction(hud.Flash));
            var look = enemy.GetComponent<DemoEnemyLook>();
            UnityEventTools.AddPersistentListener(enemy.onStateChanged, new UnityAction<HorrorState>(look.Show));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[HorrorAI] Demo scene written to " + ScenePath);
        }

        #endregion


        #region Level

        private static void SetupLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.035f, 0.05f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.015f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.05f;
        }

        private static void BuildLevel(Transform level)
        {
            var floor = Material("Floor", new Color(0.18f, 0.18f, 0.2f));
            var wall = Material("Wall", new Color(0.32f, 0.31f, 0.3f));
            var crate = Material("Crate", new Color(0.35f, 0.24f, 0.14f));
            Box(level, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 30f), floor);
            Box(level, "Wall North", new Vector3(0f, 1.5f, 15f), new Vector3(30.4f, 3f, 0.4f), wall);
            Box(level, "Wall South", new Vector3(0f, 1.5f, -15f), new Vector3(30.4f, 3f, 0.4f), wall);
            Box(level, "Wall East", new Vector3(15f, 1.5f, 0f), new Vector3(0.4f, 3f, 30f), wall);
            Box(level, "Wall West", new Vector3(-15f, 1.5f, 0f), new Vector3(0.4f, 3f, 30f), wall);
            Box(level, "Divider South", new Vector3(0f, 1.5f, -11f), new Vector3(0.4f, 3f, 8f), wall);
            Box(level, "Divider Middle", new Vector3(0f, 1.5f, 1f), new Vector3(0.4f, 3f, 12f), wall);
            Box(level, "Divider North", new Vector3(0f, 1.5f, 12.5f), new Vector3(0.4f, 3f, 5f), wall);
            Box(level, "Room Wall West", new Vector3(3.5f, 1.5f, 0f), new Vector3(7f, 3f, 0.4f), wall);
            Box(level, "Room Wall East", new Vector3(12.5f, 1.5f, 0f), new Vector3(5f, 3f, 0.4f), wall);
            Box(level, "Crate 1", new Vector3(-6f, 0.5f, -4f), new Vector3(1.6f, 1f, 1.6f), crate);
            Box(level, "Crate 2", new Vector3(-4f, 0.5f, 6f), new Vector3(2.4f, 1f, 1.2f), crate);
            Box(level, "Crate 3", new Vector3(10f, 0.5f, -5f), new Vector3(1.4f, 1f, 1.4f), crate);
            Box(level, "Crate 4", new Vector3(5f, 0.5f, 10f), new Vector3(1.2f, 1f, 2.4f), crate);
            Box(level, "Crate 5", new Vector3(-10f, 0.5f, 2f), new Vector3(1.2f, 1f, 1.2f), crate);
        }

        private static void BakeNavMesh(Transform level)
        {
            var surface = new GameObject("NavMesh").AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.transform.SetParent(level, false);
            foreach (Transform child in level)
            {
                if (child != surface.transform)
                    child.SetParent(surface.transform, true);
            }
            surface.BuildNavMesh();
            var path = Folder + "/Horror AI Demo NavMesh.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
        }

        private static void BuildLamps(Transform level)
        {
            Lamp(level, "Lamp North Room", new Vector3(8f, 2.7f, 8f), 8f, new Color(1f, 0.85f, 0.6f));
            Lamp(level, "Lamp South Room", new Vector3(7f, 2.7f, -9f), 6f, new Color(1f, 0.8f, 0.55f));
            Lamp(level, "Lamp Hall", new Vector3(-8f, 2.7f, -1f), 6f, new Color(0.7f, 0.8f, 1f));
        }

        private static void Lamp(Transform parent, string name, Vector3 position, float range, Color color)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            host.transform.position = position;
            var light = host.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = 1.6f;
            light.color = color;
            light.shadows = LightShadows.Soft;
            var source = host.AddComponent<HorrorLightSource>();
            source.linkedLight = light;
            source.range = range;
        }

        #endregion


        #region Actors

        private static PatrolRoute BuildRoute()
        {
            var route = new GameObject("Patrol Route").AddComponent<PatrolRoute>();
            route.mode = PatrolMode.Loop;
            route.AddWaypoint(new Vector3(-11f, 0f, -11f), 2f);
            route.AddWaypoint(new Vector3(-11f, 0f, 11f), 1f);
            route.AddWaypoint(new Vector3(9f, 0f, 11f), 2.5f);
            route.AddWaypoint(new Vector3(8f, 0f, 4f), 0.5f);
            route.AddWaypoint(new Vector3(-6f, 0f, -1f), 1.5f);
            return route;
        }

        private static DemoPlayer BuildPlayer()
        {
            var host = new GameObject("Player") { tag = "Player" };
            host.transform.SetPositionAndRotation(new Vector3(12f, 0.05f, -12.5f), Quaternion.Euler(0f, -45f, 0f));
            var controller = host.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            var target = host.AddComponent<HorrorTarget>();
            var head = new GameObject("Head").transform;
            head.SetParent(host.transform, false);
            head.localPosition = new Vector3(0f, 1.6f, 0f);
            var camera = head.gameObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.05f;
            head.gameObject.AddComponent<AudioListener>();
            head.gameObject.tag = "MainCamera";
            var lamp = new GameObject("Flashlight");
            lamp.transform.SetParent(head, false);
            lamp.transform.localPosition = new Vector3(0.25f, -0.2f, 0.1f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 16f;
            light.spotAngle = 50f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.Soft;
            var flashlight = lamp.AddComponent<HorrorLightSource>();
            flashlight.linkedLight = light;
            flashlight.shape = HorrorLightShape.Spot;
            flashlight.revealsHolder = true;
            flashlight.SetOn(false);
            var player = host.AddComponent<DemoPlayer>();
            player.head = head;
            player.target = target;
            player.flashlight = flashlight;
            return player;
        }

        private static HorrorEnemy BuildEnemy(PatrolRoute route)
        {
            var host = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            host.name = "Enemy";
            host.transform.position = new Vector3(-11f, 1f, -11f);
            host.GetComponent<Renderer>().sharedMaterial = Material("Enemy", new Color(0.3f, 0.75f, 0.45f));
            var agent = host.AddComponent<NavMeshAgent>();
            agent.baseOffset = 1f;
            agent.angularSpeed = 360f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 0.2f;
            var eye = new GameObject("Eye").transform;
            eye.SetParent(host.transform, false);
            eye.localPosition = new Vector3(0f, 0.7f, 0.3f);
            var glow = eye.gameObject.AddComponent<Light>();
            glow.range = 2.5f;
            glow.intensity = 2f;
            var enemy = host.AddComponent<HorrorEnemy>();
            enemy.archetype = SceneFixes.Patroller;
            enemy.patrolRoute = route;
            enemy.eye = eye;
            var look = host.AddComponent<DemoEnemyLook>();
            look.body = host.GetComponent<Renderer>();
            look.eyeLight = glow;
            return enemy;
        }

        private static void BuildProps()
        {
            var glass = Material("Bottle", new Color(0.2f, 0.45f, 0.3f));
            Bottle("Bottle 1", new Vector3(-6f, 1.25f, -4f), glass);
            Bottle("Bottle 2", new Vector3(-4.6f, 1.25f, 6f), glass);
            Bottle("Bottle 3", new Vector3(10f, 1.25f, -5f), glass);
            var creak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            creak.name = "Creaky Floor";
            creak.transform.position = new Vector3(0f, 0.05f, -5.5f);
            creak.transform.localScale = new Vector3(2.2f, 0.1f, 3f);
            creak.GetComponent<Renderer>().sharedMaterial = Material("Creaky Floor", new Color(0.28f, 0.2f, 0.14f));
            creak.GetComponent<Collider>().isTrigger = true;
            var noise = creak.AddComponent<NoiseEmitter>();
            noise.emitOn = NoiseTrigger.TriggerEnter;
            noise.loudness = 9f;
            noise.triggerTag = "Player";
            noise.cooldown = 1f;
        }

        private static void Bottle(string name, Vector3 position, Material material)
        {
            var bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.name = name;
            bottle.transform.position = position;
            bottle.transform.localScale = new Vector3(0.18f, 0.25f, 0.18f);
            bottle.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(bottle.GetComponent<Collider>());
            bottle.AddComponent<CapsuleCollider>();
            bottle.AddComponent<Rigidbody>().mass = 0.4f;
            var noise = bottle.AddComponent<NoiseEmitter>();
            noise.loudness = 12f;
            noise.minImpactSpeed = 1f;
            noise.fullImpactSpeed = 4f;
        }

        #endregion


        #region Tools and Utilities

        private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            box.isStatic = true;
        }

        private static Material Material(string name, Color color)
        {
            var path = Folder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                if (!AssetDatabase.IsValidFolder(Folder + "/Materials"))
                    AssetDatabase.CreateFolder(Folder, "Materials");
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Glossiness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        #endregion
    }
}
