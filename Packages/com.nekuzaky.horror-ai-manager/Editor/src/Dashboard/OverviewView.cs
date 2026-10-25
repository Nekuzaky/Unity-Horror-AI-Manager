using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class OverviewView : IDashboardView
    {
        #region Public

        public const float CostScale = 0.05f;

        public VisualElement Root { get; }

        public OverviewView()
        {
            Root = Ui.Box("ha-view");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("ha-scroll");
            var page = Ui.Box("ha-page");
            scroll.Add(page);
            Root.Add(scroll);
            _hint = Ui.Styled(EditHint, "ha-help");
            page.Add(_hint);
            page.Add(Ui.Section(Glyphs.Enemies, "Enemies by state"));
            page.Add(BuildStates());
            page.Add(Ui.Section(Glyphs.Cost, "Cost and perception"));
            page.Add(BuildMetrics());
            page.Add(Ui.Section(Glyphs.Time, "Last 30 seconds"));
            page.Add(BuildGraphs());
        }

        public StatCard Cost => _cost;

        public StatCard Highest => _highest;

        public StatCard Garbage => _garbage;

        public int CountOf(HorrorState state) => _counts[(int)state];

        public void Refresh(DashboardSampler sampler) => Refresh(sampler, Application.isPlaying);

        public void Refresh(DashboardSampler sampler, bool playing)
        {
            Ui.SetText(_hint, playing ? PlayHint : EditHint);
            if (!playing)
            {
                ShowScene();
                return;
            }
            sampler.Summarize();
            CountStates();
            ShowStates();
            ShowMetrics(sampler);
            _costGraph.Show(sampler.CostHistory);
            _suspicionGraph.Show(sampler.SuspicionHistory);
            Ui.SetText(_costValue, Text.Milliseconds(sampler.CostHistory.Latest));
            Ui.SetText(_suspicionValue, Text.Percent(sampler.SuspicionHistory.Latest));
        }

        public void SceneChanged()
        {
            _sceneEnemies = SceneFixes.CountEnemies();
            _sceneTargets = SceneFixes.CountTargets();
            _sceneLights = SceneFixes.CountLights();
        }

        public void ProjectChanged()
        {
        }

        #endregion


        #region Private and Protected

        private const string EditHint = "Edit Mode: press Play to watch every enemy live. Until then this page counts what is in the open scenes.";
        private const string PlayHint = "Play Mode: live data, refreshed ten times a second. Times are the Horror AI update on the main thread.";

        private static readonly string[] StateDetails =
            { "walking its round", "stopped, staring", "going to check", "searching the area", "chasing the target", "lost the target" };

        private readonly StatCard[] _states = new StatCard[6];
        private readonly int[] _counts = new int[6];
        private readonly Label _hint;
        private StatCard _highest;
        private StatCard _cost;
        private StatCard _rays;
        private StatCard _perceptions;
        private StatCard _garbage;
        private StatCard _scene;
        private Sparkline _costGraph;
        private Sparkline _suspicionGraph;
        private Label _costValue;
        private Label _suspicionValue;
        private HorrorEnemy _highestEnemy;
        private float _highestSuspicion;
        private int _sceneEnemies;
        private int _sceneTargets;
        private int _sceneLights;

        private VisualElement BuildStates()
        {
            var cards = Ui.Box("ha-cards");
            for (var i = 0; i < _states.Length; i++)
            {
                var state = (HorrorState)i;
                _states[i] = new StatCard(Glyphs.Of(state), Ui.StateName(state), StateDetails[i], "ha-card--state", Ui.StateClass(state));
                cards.Add(_states[i].Root);
            }
            return cards;
        }

        private VisualElement BuildMetrics()
        {
            var cards = Ui.Box("ha-cards");
            _highest = new StatCard(Glyphs.Suspicion, "Highest suspicion", "nobody yet");
            _cost = new StatCard(Glyphs.Cost, "AI cost per frame", "p95");
            _rays = new StatCard(Glyphs.Rays, "Rays per frame", "budget");
            _perceptions = new StatCard(Glyphs.Perceptions, "Perceptions per frame", "enemies looking and listening");
            _garbage = new StatCard(Glyphs.Garbage, "Garbage per frame", "worst of the last 600 frames");
            _scene = new StatCard(Glyphs.Target, "Enemies running", "with");
            cards.Add(_highest.Root);
            cards.Add(_cost.Root);
            cards.Add(_rays.Root);
            cards.Add(_perceptions.Root);
            cards.Add(_garbage.Root);
            cards.Add(_scene.Root);
            return cards;
        }

        private VisualElement BuildGraphs()
        {
            var graphs = Ui.Box("ha-graphs");
            _costGraph = new Sparkline(CostScale, "ha-spark--tall");
            _suspicionGraph = new Sparkline(1f, "ha-spark--tall", "ha-spark--suspicion");
            graphs.Add(GraphBox(Glyphs.Cost, "AI cost per frame", _costGraph, out _costValue));
            graphs.Add(GraphBox(Glyphs.Suspicion, "Highest suspicion", _suspicionGraph, out _suspicionValue));
            return graphs;
        }

        private static VisualElement GraphBox(string glyph, string title, Sparkline graph, out Label value)
        {
            var box = Ui.Box("ha-graph-box");
            var head = Ui.Box("ha-graph-title");
            head.Add(Glyphs.Icon(glyph, "ha-icon--small", "ha-icon--dim"));
            head.Add(Ui.Styled(title, "ha-card-label"));
            head.Add(Ui.Spacer());
            value = Ui.Styled(Text.None, "ha-graph-value");
            head.Add(value);
            box.Add(head);
            box.Add(graph);
            return box;
        }

        private void CountStates()
        {
            for (var i = 0; i < _counts.Length; i++)
                _counts[i] = 0;
            _highestEnemy = null;
            _highestSuspicion = 0f;
            var enemies = HorrorAI.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null)
                    continue;
                _counts[(int)enemy.State]++;
                if (_highestEnemy != null && enemy.Suspicion <= _highestSuspicion)
                    continue;
                _highestEnemy = enemy;
                _highestSuspicion = enemy.Suspicion;
            }
        }

        private void ShowStates()
        {
            for (var i = 0; i < _states.Length; i++)
                _states[i].Set(Text.Integer(_counts[i]));
        }

        private void ShowMetrics(DashboardSampler sampler)
        {
            var stats = HorrorAI.Stats;
            _highest.Set(_highestEnemy != null ? Text.Percent(_highestSuspicion) : Text.None, null, _highestEnemy != null ? Text.Name(_highestEnemy) : "nobody yet");
            _cost.Set(Text.Milliseconds(sampler.AverageCost), Text.Milliseconds(sampler.PeakCost));
            _rays.Set(Text.Decimal(sampler.AverageRays), Text.Integer(HorrorAI.RaycastBudget));
            _perceptions.Set(Text.Decimal(sampler.AveragePerceptions));
            var latest = sampler.FrameAllocated.Count > 0 ? (long)sampler.FrameAllocated.Latest : -1L;
            var measured = latest >= 0L;
            _garbage.Set(Text.Bytes(latest), measured ? Text.Bytes(sampler.WorstAllocated) : null, measured ? "worst of the last 600 frames" : "not measured in this build");
            _scene.Set(Text.Integer(stats.Enemies), Text.TargetsAndLights(stats.Targets, stats.Lights));
        }

        private void ShowScene()
        {
            for (var i = 0; i < _states.Length; i++)
                _states[i].Set(Text.None);
            _highest.Set(Text.None, null, "press Play");
            _cost.Set(Text.None, Text.None);
            _rays.Set(Text.None, Text.Integer(HorrorAI.RaycastBudget));
            _perceptions.Set(Text.None);
            _garbage.Set(Text.None, null, "press Play");
            _scene.Set(Text.Integer(_sceneEnemies), Text.TargetsAndLights(_sceneTargets, _sceneLights));
        }

        #endregion
    }
}
