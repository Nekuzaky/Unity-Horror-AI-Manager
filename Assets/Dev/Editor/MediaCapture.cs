using System.Reflection;
using Nekuzaky.HorrorAI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nekuzaky.HorrorAI.Dev
{
    [InitializeOnLoad]
    internal static class MediaCapture
    {
        #region Public

        public const string ScenePath = "Assets/Dev/Demo/Horror AI Demo.unity";

        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath);
            foreach (var window in Resources.FindObjectsOfTypeAll<DashboardWindow>())
                window.Close();
            Advance(Stage.Layout, 1.0);
            EditorApplication.update += Update;
        }

        #endregion


        #region Private and Protected

        private enum Stage
        {
            Idle,
            Layout,
            Enemy,
            Light,
            Archetype,
            Profiles,
            Icons,
            Project,
            Setup,
            Archetypes,
            EnterPlay,
            Record,
            Overview,
            Enemies,
            Noises,
            ExitPlay,
            Quit
        }

        private const string StageKey = "HorrorAI.Capture.Stage";
        private const float Fps = 15f;
        private const int MaxFrames = 600;
        private static readonly Rect SceneRect = new(20f, 60f, 1000f, 860f);
        private static readonly Rect DashboardRect = new(1310f, 60f, 1220f, 800f);
        private static readonly Rect PreviewRect = new(60f, 60f, 560f, 900f);

        private static Stage _stage;
        private static double _due;
        private static bool _shown;
        private static EditorWindow _preview;
        private static GameObject _broken;
        private static DemoDirector _director;
        private static int _frame;
        private static int _lastFrame = -1;
        private static float _nextShot;
        private static bool _gizmosSaved;

        static MediaCapture()
        {
            _stage = (Stage)SessionState.GetInt(StageKey, 0);
            if (_stage == Stage.Idle)
                return;
            _due = EditorApplication.timeSinceStartup + 2.0;
            EditorApplication.update += Update;
        }

        private static void Advance(Stage stage, double delay)
        {
            _stage = stage;
            _shown = false;
            _due = EditorApplication.timeSinceStartup + delay;
            SessionState.SetInt(StageKey, (int)stage);
        }

        private static void Update()
        {
            if (_stage == Stage.Idle || EditorApplication.timeSinceStartup < _due)
                return;
            switch (_stage)
            {
                case Stage.Layout: Layout(); break;
                case Stage.Enemy: Preview("inspector-enemy", 320f, Stage.Light, new Object[] { _broken.GetComponent<HorrorEnemy>() }); break;
                case Stage.Light: Preview("inspector-light", 560f, Stage.Archetype, new Object[] { Find<HorrorLightSource>("Lamp Hall"), Find<HorrorLightSource>("Flashlight") }); break;
                case Stage.Archetype: Preview("inspector-archetype", 330f, Stage.Profiles, new Object[] { SceneFixes.Patroller }); break;
                case Stage.Profiles: Preview("inspector-profiles", 1000f, Stage.Icons, new Object[] { SceneFixes.Patroller.perception, SceneFixes.Patroller.behavior }); break;
                case Stage.Icons: Icons(); break;
                case Stage.Project: Project(); break;
                case Stage.Setup: Tab(DashboardWindow.SetupTab, "dashboard-setup", Stage.Archetypes); break;
                case Stage.Archetypes: Tab(DashboardWindow.ArchetypesTab, "dashboard-archetypes", Stage.EnterPlay); break;
                case Stage.EnterPlay: EnterPlay(); break;
                case Stage.Record: Record(); break;
                case Stage.Overview: Tab(DashboardWindow.OverviewTab, "dashboard-overview", Stage.Enemies); break;
                case Stage.Enemies: Tab(DashboardWindow.EnemiesTab, "dashboard-enemies", Stage.Noises); break;
                case Stage.Noises: Tab(DashboardWindow.NoisesTab, "dashboard-noises", Stage.ExitPlay); break;
                case Stage.ExitPlay: ExitPlay(); break;
                case Stage.Quit: Quit(); break;
            }
        }

        private static void Layout()
        {
            var view = Scene();
            view.LookAt(Vector3.zero, Quaternion.Euler(90f, 0f, 0f), 15.6f, true, true);
            Dashboard();
            _broken = new GameObject("Horror Enemy (new)");
            _broken.transform.position = new Vector3(-13f, 0f, 13f);
            _broken.AddComponent<HorrorEnemy>();
            Selection.activeGameObject = _broken;
            Advance(Stage.Enemy, 2.0);
        }

        private static void Preview(string name, float height, Stage next, Object[] targets)
        {
            if (!_shown)
            {
                _preview = InspectorPreviewWindow.Show(new Rect(PreviewRect.x, PreviewRect.y, PreviewRect.width, height), "Inspector", targets);
                _shown = true;
                _due = EditorApplication.timeSinceStartup + 2.0;
                return;
            }
            CaptureKit.Save(_preview, name);
            _preview.Close();
            Advance(next, 0.5);
        }

        private static void Icons()
        {
            if (!_shown)
            {
                _preview = IconSheetWindow.Show(new Rect(60f, 60f, 960f, 280f));
                _shown = true;
                _due = EditorApplication.timeSinceStartup + 1.5;
                return;
            }
            CaptureKit.Save(_preview, "icons");
            _preview.Close();
            Advance(Stage.Project, 0.5);
        }

        private static void Project()
        {
            var browser = CaptureKit.FindWindow("UnityEditor.ProjectBrowser");
            if (!_shown)
            {
                browser.position = new Rect(60f, 60f, 900f, 420f);
                typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser")?
                    .GetMethod("SetTwoColumns", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(browser, null);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Packages/com.nekuzaky.horror-ai-manager/Runtime/src/Components/HorrorEnemy.cs");
                Selection.activeObject = script;
                EditorGUIUtility.PingObject(script);
                _shown = true;
                _due = EditorApplication.timeSinceStartup + 2.0;
                return;
            }
            CaptureKit.Save(browser, "icons-project");
            Selection.activeGameObject = _broken;
            Advance(Stage.Setup, 1.0);
        }

        private static void Tab(int tab, string name, Stage next)
        {
            var window = Dashboard();
            if (!_shown)
            {
                window.ShowTab(tab);
                window.RefreshNow();
                _shown = true;
                _due = EditorApplication.timeSinceStartup + 1.2;
                return;
            }
            window.RefreshNow();
            CaptureKit.Save(window, name);
            Advance(next, 0.5);
        }

        private static void EnterPlay()
        {
            if (_broken != null)
                Object.DestroyImmediate(_broken);
            Dashboard().ShowTab(DashboardWindow.OverviewTab);
            ShowNavMesh(false);
            CaptureKit.ResetFrames("hero");
            CaptureKit.ResetFrames("dashboard");
            Time.captureFramerate = (int)Fps;
            EditorApplication.isPaused = true;
            Advance(Stage.Record, 3.0);
            EditorApplication.EnterPlaymode();
        }

        private static void Record()
        {
            if (!Application.isPlaying)
                return;
            if (_director == null)
            {
                StartDirector();
                EditorApplication.Step();
                return;
            }
            if (Time.frameCount == _lastFrame)
                return;
            _lastFrame = Time.frameCount;
            _director.Step(Time.deltaTime);
            Follow();
            if (Time.time < _nextShot)
            {
                EditorApplication.Step();
                return;
            }
            _nextShot = Mathf.Max(_nextShot + 1f / Fps, Time.time);
            CaptureKit.SaveFrame(Scene(), "hero", _frame);
            CaptureKit.SaveFrame(Dashboard(), "dashboard", _frame);
            _frame++;
            if (_frame % 60 == 0)
                Debug.Log("[Capture] frame " + _frame + ", game time " + Time.time.ToString("0.0") + " s, " + _director.Current + ", enemy " + _director.Enemy.State);
            if (_director.Current == DemoDirector.Phase.Done || _frame > MaxFrames)
            {
                Debug.Log("Recorded " + _frame + " frames, enemy " + _director.Enemy.State);
                Time.captureFramerate = 0;
                EditorApplication.isPaused = false;
                Advance(Stage.Overview, 0.5);
                return;
            }
            EditorApplication.Step();
        }

        private static void StartDirector()
        {
            _director = new DemoDirector();
            if (!_director.Start())
            {
                Debug.LogError("[Capture] the demo scene is missing the enemy, the player or the bottle");
                Advance(Stage.ExitPlay, 0.0);
                return;
            }
            Time.captureFramerate = (int)Fps;
            Tools.hidden = true;
            ShowNavMesh(false);
            EditorApplication.isPaused = true;
            LockInspector(true);
            Selection.activeGameObject = _director.Enemy.gameObject;
            Scene().LookAt(Vector3.zero, Quaternion.Euler(90f, 0f, 0f), 15.6f, true, true);
            Dashboard().ShowTab(DashboardWindow.OverviewTab);
        }

        private static void Follow()
        {
            var dashboard = Dashboard();
            if (_director.Current >= DemoDirector.Phase.Approach && dashboard.Tab != DashboardWindow.EnemiesTab)
                dashboard.ShowTab(DashboardWindow.EnemiesTab);
            if (_gizmosSaved || _director.Enemy.State != HorrorState.Chase || _director.Enemy.StateTime < 0.8f)
                return;
            _gizmosSaved = CaptureKit.Save(Scene(), "scene-gizmos");
        }

        private static void ExitPlay()
        {
            Time.captureFramerate = 0;
            Advance(Stage.Quit, 3.0);
            EditorApplication.ExitPlaymode();
        }

        private static void ShowNavMesh(bool show)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("UnityEditor.AI.NavMeshVisualizationSettings");
                var property = type?.GetProperty("showNavMesh", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (property == null)
                    continue;
                property.SetValue(null, show);
                return;
            }
            Debug.LogWarning("[Capture] could not hide the NavMesh");
        }

        private static void LockInspector(bool locked)
        {
            var inspector = CaptureKit.FindWindow("UnityEditor.InspectorWindow");
            var property = inspector?.GetType().GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            property?.SetValue(inspector, locked);
        }

        private static void Quit()
        {
            LockInspector(false);
            Tools.hidden = false;
            ShowNavMesh(true);
            _stage = Stage.Idle;
            SessionState.EraseInt(StageKey);
            foreach (var window in Resources.FindObjectsOfTypeAll<DashboardWindow>())
                window.Close();
            EditorApplication.Exit(0);
        }

        private static SceneView Scene()
        {
            var view = EditorWindow.GetWindow<SceneView>();
            if (view.position != SceneRect)
                view.position = SceneRect;
            view.drawGizmos = true;
            view.showGrid = false;
            view.sceneLighting = false;
            view.overlayCanvas.overlaysEnabled = false;
            return view;
        }

        private static DashboardWindow Dashboard()
        {
            var windows = Resources.FindObjectsOfTypeAll<DashboardWindow>();
            var window = windows.Length > 0 ? windows[0] : DashboardWindow.Open();
            if (window.position != DashboardRect)
                window.position = DashboardRect;
            return window;
        }

        private static T Find<T>(string name) where T : Component
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<T>() : null;
        }

        #endregion
    }
}
