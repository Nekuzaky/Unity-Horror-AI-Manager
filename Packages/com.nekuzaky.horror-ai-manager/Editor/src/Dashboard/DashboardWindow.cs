using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class DashboardWindow : EditorWindow
    {
        #region Public

        public const string Title = "Horror AI";
        public const string MenuRoot = "Tools/Horror AI/";
        public const string DocumentationUrl = "https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/index.md";
        public const string DashboardUrl = "https://github.com/Nekuzaky/Unity-Horror-AI-Manager/blob/main/Packages/com.nekuzaky.horror-ai-manager/Documentation~/dashboard.md";
        public const string RepositoryUrl = "https://github.com/Nekuzaky/Unity-Horror-AI-Manager";
        public const long RefreshMilliseconds = 100L;
        public const double SceneDelay = 0.5d;

        public const int OverviewTab = 0;
        public const int EnemiesTab = 1;
        public const int NoisesTab = 2;
        public const int SetupTab = 3;
        public const int ArchetypesTab = 4;

        [MenuItem(MenuRoot + "Dashboard %#&h", priority = 1)]
        public static DashboardWindow Open()
        {
            var window = GetWindow<DashboardWindow>();
            window.minSize = new Vector2(420f, 260f);
            window.Show();
            window.Build();
            return window;
        }

        [MenuItem(MenuRoot + "Documentation", priority = 40)]
        public static void OpenDocumentation() => Application.OpenURL(DocumentationUrl);

        [MenuItem(MenuRoot + "GitHub Repository", priority = 41)]
        public static void OpenRepository() => Application.OpenURL(RepositoryUrl);

        public DashboardSampler Sampler => _sampler;

        public int Tab => _tab;

        public bool Paused => _paused;

        public IReadOnlyList<IDashboardView> Views => _views;

        public OverviewView Overview => _overview;

        public EnemiesView EnemiesPage => _enemies;

        public NoisesView NoisesPage => _noises;

        public DoctorView SetupPage => _doctor;

        public ArchetypesView ArchetypesPage => _archetypes;

        public void ShowTab(int index)
        {
            if (_views.Count == 0)
                return;
            _tab = Mathf.Clamp(index, 0, _views.Count - 1);
            for (var i = 0; i < _views.Count; i++)
            {
                _views[i].Root.style.display = i == _tab ? DisplayStyle.Flex : DisplayStyle.None;
                _tabs[i].EnableInClassList("ha-tab--active", i == _tab);
            }
            _views[_tab].Refresh(_sampler);
        }

        public void RefreshNow()
        {
            if (_views.Count == 0)
                return;
            UpdateHeader();
            if (_paused)
                return;
            if (_sceneDirtyAt > 0d && EditorApplication.timeSinceStartup >= _sceneDirtyAt)
                NotifyScene();
            _views[_tab].Refresh(_sampler);
        }

        #endregion


        #region Private and Protected

        [SerializeField] private int _tab;
        [SerializeField] private bool _follow = true;

        private readonly List<IDashboardView> _views = new();
        private readonly List<Button> _tabs = new();
        private readonly DashboardSampler _sampler = new();
        private OverviewView _overview;
        private EnemiesView _enemies;
        private NoisesView _noises;
        private DoctorView _doctor;
        private ArchetypesView _archetypes;
        private Label _modePill;
        private Button _pauseButton;
        private VisualElement _pauseIcon;
        private Button _followButton;
        private bool _paused;
        private bool _visible = true;
        private bool _wasPlaying;
        private double _sceneDirtyAt = 1d;

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            titleContent = new GUIContent(Title, Glyphs.Tile("HorrorAI"), "Enemies, noises, setup and archetypes of Horror AI Manager");
            _sampler.Attach();
            EditorApplication.hierarchyChanged += MarkScene;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.newSceneCreated += OnSceneCreated;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            _sampler.Detach();
            _sampler.Clear();
            EditorApplication.hierarchyChanged -= MarkScene;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.newSceneCreated -= OnSceneCreated;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnBecameVisible() => _visible = true;

        private void OnBecameInvisible() => _visible = false;

        private void OnProjectChange()
        {
            for (var i = 0; i < _views.Count; i++)
                _views[i].ProjectChanged();
        }

        private void CreateGUI() => Build();

        private void Build()
        {
            if (_views.Count > 0)
                return;
            var root = rootVisualElement;
            var sheet = Glyphs.StyleSheet;
            if (sheet != null)
                root.styleSheets.Add(sheet);
            root.AddToClassList("ha-root");
            root.EnableInClassList("ha-light", !EditorGUIUtility.isProSkin);
            _overview = new OverviewView();
            _enemies = new EnemiesView();
            _noises = new NoisesView();
            _doctor = new DoctorView();
            _archetypes = new ArchetypesView();
            _views.Clear();
            _views.Add(_overview);
            _views.Add(_enemies);
            _views.Add(_noises);
            _views.Add(_doctor);
            _views.Add(_archetypes);
            root.Add(BuildHeader());
            var body = Ui.Box("ha-body");
            for (var i = 0; i < _views.Count; i++)
                body.Add(_views[i].Root);
            root.Add(body);
            NotifyScene();
            ShowTab(_tab);
            root.schedule.Execute(Tick).Every(RefreshMilliseconds);
        }

        #endregion


        #region Header

        private VisualElement BuildHeader()
        {
            var header = Ui.Box("ha-header");
            header.Add(Glyphs.TileImage("HorrorAI", "ha-tile--logo"));
            header.Add(Ui.Styled(Title, "ha-title"));
            AddTab(header, Glyphs.Overview, "Overview", OverviewTab);
            AddTab(header, Glyphs.Enemies, "Enemies", EnemiesTab);
            AddTab(header, Glyphs.Noises, "Noises", NoisesTab);
            AddTab(header, Glyphs.Setup, "Setup", SetupTab);
            AddTab(header, Glyphs.Archetypes, "Archetypes", ArchetypesTab);
            header.Add(Ui.Spacer());
            _modePill = Ui.Styled("Edit Mode", "ha-pill");
            _modePill.tooltip = "Live data needs Play Mode. Setup and Archetypes work in both.";
            header.Add(_modePill);
            _pauseButton = Ui.IconButton(Glyphs.Pause, "Pause the updates (the data keeps being recorded)", TogglePause, "ha-button--flat");
            _pauseIcon = _pauseButton[0];
            header.Add(_pauseButton);
            _followButton = Ui.IconButton(Glyphs.Follow, "Follow the selection: picking an enemy in the scene picks it here", ToggleFollow, "ha-button--flat");
            _followButton.EnableInClassList("ha-button--on", _follow);
            header.Add(_followButton);
            header.Add(Ui.IconButton(Glyphs.Docs, "How to read the dashboard", () => Application.OpenURL(DashboardUrl), "ha-button--flat"));
            header.Add(Ui.IconButton(Glyphs.GitHub, "GitHub repository", OpenRepository, "ha-button--flat"));
            return header;
        }

        private void AddTab(VisualElement header, string glyph, string text, int index)
        {
            var tab = new Button(() => ShowTab(index));
            tab.AddToClassList("ha-tab");
            tab.Add(Glyphs.Icon(glyph));
            tab.Add(new Label(text));
            header.Add(tab);
            _tabs.Add(tab);
        }

        private void UpdateHeader()
        {
            var playing = Application.isPlaying;
            Ui.SetText(_modePill, _paused ? "Paused" : playing ? "Play Mode" : "Edit Mode");
            _modePill.EnableInClassList("ha-pill--live", playing && !_paused);
            _modePill.EnableInClassList("ha-pill--paused", _paused);
        }

        private void TogglePause()
        {
            _paused = !_paused;
            Glyphs.Set(_pauseIcon, _paused ? Glyphs.Resume : Glyphs.Pause);
            _pauseButton.tooltip = _paused ? "Resume the updates" : "Pause the updates (the data keeps being recorded)";
            _pauseButton.EnableInClassList("ha-button--on", _paused);
            RefreshNow();
        }

        private void ToggleFollow()
        {
            _follow = !_follow;
            _followButton.EnableInClassList("ha-button--on", _follow);
            if (_follow)
                OnSelectionChanged();
        }

        #endregion


        #region Events

        private void Tick()
        {
            var playing = Application.isPlaying;
            if (playing != _wasPlaying)
            {
                _wasPlaying = playing;
                MarkScene();
            }
            if (!_visible)
                return;
            RefreshNow();
        }

        private void MarkScene()
        {
            if (_sceneDirtyAt <= 0d)
                _sceneDirtyAt = EditorApplication.timeSinceStartup + SceneDelay;
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode mode) => NotifyScene();

        private void OnSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode) => NotifyScene();

        private void OnActiveSceneChanged(Scene previous, Scene next) => NotifyScene();

        private void OnSceneClosed(Scene scene) => MarkScene();

        private void NotifyScene()
        {
            _sceneDirtyAt = 0d;
            Text.ForgetNames();
            for (var i = 0; i < _views.Count; i++)
                _views[i].SceneChanged();
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode || change == PlayModeStateChange.EnteredEditMode)
            {
                _sampler.Clear();
                _sampler.Attach();
                MarkScene();
            }
        }

        private void OnSelectionChanged()
        {
            if (!_follow || _enemies == null)
                return;
            var go = Selection.activeGameObject;
            if (go != null && go.TryGetComponent(out HorrorEnemy enemy))
                _enemies.Follow(enemy);
        }

        #endregion
    }
}
