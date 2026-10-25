using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class EnemiesView : IDashboardView
    {
        #region Public

        public const float RowHeight = 28f;

        public VisualElement Root { get; }

        public EnemiesView()
        {
            Root = Ui.Box("ha-view", "ha-split");
            var left = Ui.Box("ha-list-pane");
            left.Add(Header());
            _list = new ListView
            {
                itemsSource = _items,
                fixedItemHeight = RowHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.Single,
                makeItem = () => new EnemyRow().Root,
                bindItem = Bind,
                unbindItem = Unbind
            };
            _list.style.flexGrow = 1f;
            _list.selectedIndicesChanged += OnPicked;
            left.Add(_list);
            _hint = Ui.Styled(string.Empty, "ha-help");
            _hint.style.marginLeft = 10f;
            left.Add(_hint);
            Root.Add(left);
            var right = new ScrollView(ScrollViewMode.Vertical);
            right.AddToClassList("ha-detail-pane");
            right.Add(_detail.Root);
            Root.Add(right);
            Root.RegisterCallback<GeometryChangedEvent>(OnResized);
        }

        public IReadOnlyList<HorrorEnemy> Items => _items;

        public IReadOnlyList<EnemyRow> BoundRows => _bound;

        public EnemyDetail Detail => _detail;

        public HorrorEnemy Selected => _selected;

        public bool Narrow => Root.ClassListContains(NarrowClass);

        public void Resize(float width) => Root.EnableInClassList(NarrowClass, width <= NarrowWidth);

        public void Refresh(DashboardSampler sampler)
        {
            var playing = Application.isPlaying;
            if (playing != _playing)
            {
                _playing = playing;
                SceneChanged();
            }
            if (Collect())
                _list.RefreshItems();
            for (var i = 0; i < _bound.Count; i++)
                _bound[i].Update(SetupOf(_bound[i].Enemy));
            if (_selected == null && _items.Count > 0 && _autoPicked == false)
            {
                _autoPicked = true;
                _selected = _items[0];
            }
            _detail.Show(_selected, sampler);
            Ui.SetText(_hint, _items.Count == 0 ? (playing ? NoneRunning : NoneInScene) : string.Empty);
        }

        public void Follow(HorrorEnemy enemy)
        {
            if (enemy == null)
                return;
            _selected = enemy;
            var index = _items.IndexOf(enemy);
            if (index < 0)
                return;
            _single[0] = index;
            _list.SetSelectionWithoutNotify(_single);
            _list.ScrollToItem(index);
        }

        public void SceneChanged()
        {
            _setup.Clear();
            _detail.Invalidate();
            if (_selected == null)
            {
                _selected = null;
                _autoPicked = false;
            }
            _scene = Application.isPlaying ? null : Object.FindObjectsByType<HorrorEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            if (_scene == null)
                return;
            for (var i = 0; i < _scene.Length; i++)
                _setup[_scene[i]] = SetupSummary(_scene[i]);
        }

        public void ProjectChanged()
        {
        }

        #endregion


        #region Private and Protected

        private const string NarrowClass = "ha-split--narrow";
        private const float NarrowWidth = 600f;
        private const string NoneRunning = "No enemy is running. Enemies register when they are enabled in Play Mode.";
        private const string NoneInScene = "No Horror Enemy in the open scenes. Add one with GameObject > Horror AI > Enemy.";

        private readonly List<HorrorEnemy> _items = new(64);
        private readonly List<EnemyRow> _bound = new(64);
        private readonly Dictionary<HorrorEnemy, string> _setup = new(64);
        private readonly int[] _single = new int[1];
        private readonly EnemyDetail _detail = new();
        private readonly ListView _list;
        private readonly Label _hint;
        private HorrorEnemy[] _scene;
        private HorrorEnemy _selected;
        private bool _playing;
        private bool _autoPicked;

        private static VisualElement Header()
        {
            var header = Ui.Box("ha-table-header");
            var spacer = Ui.Box("ha-tile");
            spacer.style.height = 1f;
            header.Add(spacer);
            header.Add(Ui.Styled("Enemy", "ha-col-name"));
            header.Add(Ui.Styled("State", "ha-col-state"));
            header.Add(Ui.Styled("Suspicion", "ha-col-bar"));
            header.Add(Ui.Styled("Target", "ha-col-target"));
            header.Add(Ui.Styled("Distance", "ha-col-distance"));
            header.Add(Ui.Styled("Last change, because it...", "ha-col-reason"));
            return header;
        }

        private bool Collect()
        {
            if (_playing)
                return Sync(HorrorAI.Enemies, HorrorAI.Enemies.Count);
            return _scene != null ? Sync(_scene, _scene.Length) : Sync(_items, 0);
        }

        private bool Sync(IReadOnlyList<HorrorEnemy> source, int count)
        {
            var kept = 0;
            var changed = false;
            for (var i = 0; i < count; i++)
            {
                var enemy = source[i];
                if (enemy == null)
                    continue;
                changed |= kept >= _items.Count || !ReferenceEquals(enemy, _items[kept]);
                kept++;
            }
            changed |= kept != _items.Count;
            if (!changed)
                return false;
            _items.Clear();
            for (var i = 0; i < count; i++)
            {
                if (source[i] != null)
                    _items.Add(source[i]);
            }
            return true;
        }

        private void Bind(VisualElement element, int index)
        {
            var row = (EnemyRow)element.userData;
            row.Bind(index >= 0 && index < _items.Count ? _items[index] : null);
            if (!_bound.Contains(row))
                _bound.Add(row);
            row.Update(SetupOf(row.Enemy));
        }

        private void Unbind(VisualElement element, int index) => _bound.Remove((EnemyRow)element.userData);

        private void OnResized(GeometryChangedEvent change) => Resize(change.newRect.width);

        private void OnPicked(IEnumerable<int> indices)
        {
            var index = _list.selectedIndex;
            if (index < 0 || index >= _items.Count || _items[index] == null)
                return;
            _selected = _items[index];
            Explain.SelectAndFrame(_selected.gameObject);
        }

        private string SetupOf(HorrorEnemy enemy) => enemy != null && _setup.TryGetValue(enemy, out var text) ? text : null;

        private static string SetupSummary(HorrorEnemy enemy)
        {
            var warnings = new Warnings();
            EnemyInspector.Collect(enemy, warnings);
            return warnings.Count == 0 ? "ready to run" : warnings.Count == 1 ? "1 thing to check (Setup tab)" : warnings.Count + " things to check (Setup tab)";
        }

        #endregion
    }
}
