using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class EnemyDetail
    {
        #region Public

        public readonly VisualElement Root;

        public EnemyDetail()
        {
            Root = Ui.Box("ha-detail");
            _empty = Ui.Empty("No enemy picked", "Pick an enemy in the list to see what it perceives, what it remembers and why it changed its mind.");
            Root.Add(_empty);
            _content = Ui.Box();
            Root.Add(_content);
            _content.Add(BuildHead());
            _setup = Ui.Styled(string.Empty, "ha-help");
            _content.Add(_setup);
            BuildPerception();
            BuildMemory();
            _content.Add(Ui.Section(Glyphs.Suspicion, "Suspicion, last 30 seconds"));
            _graph = new Sparkline(1f, "ha-spark--suspicion");
            _content.Add(_graph);
            _content.Add(Ui.Section(Glyphs.Memory, "Changes of mind, newest first"));
            BuildTimeline();
            Show(null, null);
        }

        public HorrorEnemy Enemy { get; private set; }

        public int VisibleTransitions => _shown;

        public void Show(HorrorEnemy enemy, DashboardSampler sampler)
        {
            Enemy = enemy;
            var has = enemy != null;
            Ui.SetVisible(_empty, !has);
            Ui.SetVisible(_content, has);
            if (!has)
                return;
            Ui.SetText(_name, Text.Name(enemy));
            var playing = enemy.IsActive;
            Ui.SetVisible(_live, playing);
            if (!playing && !ReferenceEquals(_setupFor, enemy))
            {
                _setupFor = enemy;
                _setupText = SetupText(enemy);
            }
            Ui.SetText(_setup, playing ? string.Empty : _setupText);
            if (!playing)
            {
                ShowChip(-1);
                _graph.Show(null);
                return;
            }
            ShowChip((int)enemy.State);
            ShowPerception(enemy);
            ShowMemory(enemy);
            var track = sampler?.TrackOf(enemy);
            _graph.Show(track?.Suspicion);
            ShowTimeline(track);
        }

        public void Invalidate() => _setupFor = null;

        #endregion


        #region Private and Protected

        private const string Idle = "Press Play to see it perceive, remember and change its mind.";
        private const int Rows = EnemyTrack.HistoryLength;

        private readonly VisualElement _empty;
        private readonly VisualElement _content;
        private readonly Label _setup;
        private readonly Sparkline _graph;
        private readonly TimelineRow[] _rows = new TimelineRow[Rows];
        private VisualElement _live;
        private Label _name;
        private Label _chip;
        private string _chipClass;
        private int _chipState = -3;
        private KeyValue _sees;
        private KeyValue _fills;
        private KeyValue _light;
        private KeyValue _crouch;
        private KeyValue _distance;
        private KeyValue _heard;
        private KeyValue _position;
        private KeyValue _age;
        private KeyValue _certainty;
        private KeyValue _source;
        private Vector3Int _lastPosition = new(int.MinValue, 0, 0);
        private int _shown;
        private HorrorEnemy _setupFor;
        private string _setupText = string.Empty;

        private sealed class TimelineRow
        {
            public VisualElement Root;
            public Label Time;
            public Label From;
            public Label To;
            public Label Reason;
            public string FromClass;
            public string ToClass;
        }

        private VisualElement BuildHead()
        {
            var head = Ui.Box("ha-detail-head");
            head.Add(Glyphs.TileImage("HorrorEnemy", "ha-tile--large"));
            _name = Ui.Styled(string.Empty, "ha-detail-name");
            head.Add(_name);
            _chip = Ui.Styled(string.Empty, "ha-chip");
            head.Add(_chip);
            head.Add(Ui.Spacer());
            head.Add(Ui.TextButton(Glyphs.Ping, "Frame", () => Explain.SelectAndFrame(Enemy != null ? Enemy.gameObject : null),
                "Select it and frame it in the Scene view"));
            return head;
        }

        private void BuildPerception()
        {
            _live = Ui.Box();
            _content.Add(_live);
            _live.Add(Ui.Section(Glyphs.Perceptions, "What it perceives"));
            _sees = new KeyValue(_live, "Sees the target");
            _fills = new KeyValue(_live, "Gauge full in");
            _light = new KeyValue(_live, "Light on the target");
            _crouch = new KeyValue(_live, "Target crouching");
            _distance = new KeyValue(_live, "Distance to target");
            _heard = new KeyValue(_live, "Last noise heard");
        }

        private void BuildMemory()
        {
            _live.Add(Ui.Section(Glyphs.Memory, "What it remembers"));
            _position = new KeyValue(_live, "Last known position");
            _age = new KeyValue(_live, "Remembered");
            _certainty = new KeyValue(_live, "Certainty");
            _source = new KeyValue(_live, "How it knows");
        }

        private void BuildTimeline()
        {
            for (var i = 0; i < Rows; i++)
            {
                var row = new TimelineRow { Root = Ui.Box("ha-timeline-row") };
                row.Time = Ui.Styled(string.Empty, "ha-timeline-time");
                row.From = Ui.Styled(string.Empty, "ha-chip");
                row.To = Ui.Styled(string.Empty, "ha-chip");
                row.Reason = Ui.Styled(string.Empty, "ha-timeline-reason");
                row.Root.Add(row.Time);
                row.Root.Add(row.From);
                row.Root.Add(Ui.Styled("to", "ha-timeline-arrow"));
                row.Root.Add(row.To);
                row.Root.Add(row.Reason);
                row.Root.style.display = DisplayStyle.None;
                _rows[i] = row;
                _content.Add(row.Root);
            }
        }

        private void ShowChip(int state)
        {
            if (state == _chipState)
                return;
            _chipState = state;
            Ui.SetText(_chip, state < 0 ? EnemyRow.NotRunning : Ui.StateNames[state]);
            Ui.SwapClass(_chip, ref _chipClass, state < 0 ? null : Ui.StateClasses[state]);
        }

        private void ShowPerception(HorrorEnemy enemy)
        {
            var perception = enemy.Perception;
            var target = enemy.Target;
            _sees.Set(perception.Sees ? Text.Percent(perception.VisibleFraction) : "no");
            _fills.Set(perception.Sees ? Explain.FillTime(perception.GainRate) : Text.None);
            _light.Set(target != null ? Text.Percent(target.Exposure) : Text.None);
            _crouch.Set(target == null ? Text.None : target.Crouching ? "yes" : "no");
            _distance.Set(target != null ? Text.Meters(Vector3.Distance(enemy.transform.position, target.transform.position)) : Text.None);
            _heard.Set(perception.HeardTime > 0f ? Text.Ago(HorrorAI.Time - perception.HeardTime) : "nothing yet");
        }

        private void ShowMemory(HorrorEnemy enemy)
        {
            var memory = enemy.Memory;
            if (!memory.HasMemory)
            {
                _position.Set(Text.None);
                _lastPosition = new Vector3Int(int.MinValue, 0, 0);
                _age.Set(Text.None);
                _certainty.Set(Text.Percent(0f));
                _source.Set(Explain.Source(MemorySource.None));
                return;
            }
            var key = Vector3Int.RoundToInt(memory.Position * 10f);
            if (key != _lastPosition)
            {
                _lastPosition = key;
                _position.Set(Text.Position(memory.Position));
            }
            _age.Set(Text.Ago(HorrorAI.Time - memory.Time));
            _certainty.Set(Text.Percent(memory.Certainty));
            _source.Set(Explain.Source(memory.Source));
        }

        private void ShowTimeline(EnemyTrack track)
        {
            var count = track != null ? track.TransitionCount : 0;
            for (var i = 0; i < Rows; i++)
            {
                var row = _rows[i];
                var visible = i < count;
                Ui.SetVisible(row.Root, visible);
                if (!visible)
                    continue;
                var transition = track.Transition(i);
                Ui.SetText(row.Time, Text.Ago(HorrorAI.Time - transition.Time));
                Ui.SetText(row.From, Ui.StateName(transition.From));
                Ui.SwapClass(row.From, ref row.FromClass, Ui.StateClass(transition.From));
                Ui.SetText(row.To, Ui.StateName(transition.To));
                Ui.SwapClass(row.To, ref row.ToClass, Ui.StateClass(transition.To));
                Ui.SetText(row.Reason, transition.Explanation);
            }
            _shown = count;
        }

        private static string SetupText(HorrorEnemy enemy)
        {
            var archetype = enemy.archetype != null ? "Archetype " + enemy.archetype.name : "No archetype (built-in defaults)";
            var route = enemy.patrolRoute != null ? ", patrol route " + enemy.patrolRoute.name : ", no patrol route";
            return archetype + route + ". " + Idle;
        }

        #endregion
    }
}
