using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class NoisesView : IDashboardView
    {
        #region Public

        public VisualElement Root { get; }

        public NoisesView()
        {
            Root = Ui.Box("ha-view");
            var page = Ui.Box("ha-page");
            page.style.flexGrow = 1f;
            _hint = Ui.Styled(EditHint, "ha-help");
            page.Add(_hint);
            page.Add(Header());
            _list = new ListView
            {
                itemsSource = _items,
                fixedItemHeight = EnemiesView.RowHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.Single,
                makeItem = MakeRow,
                bindItem = Bind,
                unbindItem = Unbind
            };
            _list.style.flexGrow = 1f;
            _list.selectedIndicesChanged += OnPicked;
            page.Add(_list);
            Root.Add(page);
        }

        public IReadOnlyList<int> Items => _items;

        public void Refresh(DashboardSampler sampler)
        {
            var playing = Application.isPlaying;
            Ui.SetText(_hint, playing ? (_items.Count == 0 ? QuietHint : PlayHint) : EditHint);
            _sampler = sampler;
            if (sampler.HearingVersion != _hearingVersion)
            {
                _hearingVersion = sampler.HearingVersion;
                _heardBy.Clear();
            }
            if (Collect(playing))
                _list.RefreshItems();
            for (var i = 0; i < _bound.Count; i++)
                Update(_bound[i]);
        }

        public string HeardBy(int sequence)
        {
            if (_heardBy.TryGetValue(sequence, out var text))
                return text;
            text = Describe(sequence);
            _heardBy[sequence] = text;
            return text;
        }

        public void SceneChanged()
        {
        }

        public void ProjectChanged()
        {
        }

        #endregion


        #region Private and Protected

        private const string EditHint = "Noises appear here in Play Mode: footsteps, falling bottles, anything sent with HorrorAI.EmitNoise or a Noise Emitter.";
        private const string PlayHint = "The last noises, newest first, and who heard them after distance and walls. Click one to show it in the Scene view.";
        private const string QuietHint = "No noise yet. Make one: walk, run, knock something over.";

        private sealed class Row
        {
            public VisualElement Root;
            public Label Time;
            public Label Source;
            public Label Loudness;
            public Label Position;
            public Label Heard;
            public int Sequence = -1;
        }

        private readonly List<int> _items = new(HorrorWorld.NoiseCapacity);
        private readonly List<Row> _bound = new(32);
        private readonly Dictionary<int, string> _heardBy = new(HorrorWorld.NoiseCapacity);
        private readonly StringBuilder _builder = new(256);
        private readonly ListView _list;
        private readonly Label _hint;
        private DashboardSampler _sampler;
        private int _hearingVersion = -1;
        private int _lastSequence = -1;
        private HorrorWorld _world;

        private static VisualElement Header()
        {
            var header = Ui.Box("ha-table-header");
            header.Add(Ui.Styled("When", "ha-col-time"));
            header.Add(Ui.Styled("Source", "ha-col-source"));
            header.Add(Ui.Styled("Loudness", "ha-col-loud"));
            header.Add(Ui.Styled("Where", "ha-col-position"));
            header.Add(Ui.Styled("Heard by (level after distance and walls)", "ha-col-heard"));
            return header;
        }

        private VisualElement MakeRow()
        {
            var row = new Row { Root = Ui.Box("ha-row") };
            row.Root.userData = row;
            row.Time = Ui.Styled(string.Empty, "ha-col-time");
            row.Source = Ui.Styled(string.Empty, "ha-col-source");
            row.Loudness = Ui.Styled(string.Empty, "ha-col-loud");
            row.Position = Ui.Styled(string.Empty, "ha-col-position");
            row.Heard = Ui.Styled(string.Empty, "ha-col-heard");
            row.Root.Add(row.Time);
            row.Root.Add(row.Source);
            row.Root.Add(row.Loudness);
            row.Root.Add(row.Position);
            row.Root.Add(row.Heard);
            return row.Root;
        }

        private bool Collect(bool playing)
        {
            var world = playing ? HorrorAI.World : null;
            var last = world != null ? world.NoiseSequence : -1;
            if (ReferenceEquals(world, _world) && last == _lastSequence)
                return false;
            _world = world;
            _lastSequence = last;
            _items.Clear();
            for (var i = 0; world != null && i < HorrorWorld.NoiseCapacity; i++)
            {
                if (!world.TryGetNoise(last - 1 - i, out _))
                    break;
                _items.Add(last - 1 - i);
            }
            return true;
        }

        private void Bind(VisualElement element, int index)
        {
            var row = (Row)element.userData;
            row.Sequence = index >= 0 && index < _items.Count ? _items[index] : -1;
            if (!_bound.Contains(row))
                _bound.Add(row);
            row.Position.text = string.Empty;
            Update(row);
        }

        private void Unbind(VisualElement element, int index) => _bound.Remove((Row)element.userData);

        private void Update(Row row)
        {
            if (_world == null || !_world.TryGetNoise(row.Sequence, out var noise))
            {
                Ui.SetText(row.Time, Text.None);
                return;
            }
            Ui.SetText(row.Time, Text.Ago(_world.Now - noise.Time));
            Ui.SetText(row.Source, noise.Source != null ? Text.Name(noise.Source) : "a script");
            Ui.SetText(row.Loudness, Text.Meters(noise.Loudness));
            if (row.Position.text.Length == 0)
                row.Position.text = Text.Position(noise.Position);
            Ui.SetText(row.Heard, HeardBy(row.Sequence));
        }

        private string Describe(int sequence)
        {
            if (_sampler == null)
                return Text.None;
            _builder.Clear();
            var missed = 0;
            for (var i = 0; i < _sampler.HearingCount; i++)
            {
                var record = _sampler.Hearing(i);
                if (record.Noise != sequence || record.Enemy == null)
                    continue;
                if (!record.Heard)
                {
                    missed++;
                    continue;
                }
                if (_builder.Length > 0)
                    _builder.Append(", ");
                _builder.Append(Text.Name(record.Enemy)).Append(' ').Append(Text.Percent(record.Level));
                if (record.Walls > 0)
                    _builder.Append(" (").Append(record.Walls.ToString(CultureInfo.InvariantCulture)).Append(record.Walls == 1 ? " wall)" : " walls)");
            }
            if (_builder.Length == 0)
                _builder.Append("nobody");
            if (missed > 0)
                _builder.Append("; too faint for ").Append(missed.ToString(CultureInfo.InvariantCulture));
            return _builder.ToString();
        }

        private void OnPicked(IEnumerable<int> indices)
        {
            var index = _list.selectedIndex;
            if (_world == null || index < 0 || index >= _items.Count || !_world.TryGetNoise(_items[index], out var noise))
                return;
            NoisePing.Show(noise.Position, noise.Loudness);
            if (noise.Source != null)
                EditorGUIUtility.PingObject(noise.Source);
        }

        #endregion
    }
}
