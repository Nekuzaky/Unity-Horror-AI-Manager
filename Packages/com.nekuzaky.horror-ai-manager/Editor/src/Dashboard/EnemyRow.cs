using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class EnemyRow
    {
        #region Public

        public const string NotRunning = "Not running";

        public readonly VisualElement Root;

        public EnemyRow()
        {
            Root = Ui.Box("ha-row");
            Root.userData = this;
            Root.Add(Glyphs.TileImage("HorrorEnemy", "ha-tile"));
            _name = Ui.Styled(string.Empty, "ha-col-name");
            Root.Add(_name);
            var state = Ui.Box("ha-col-state");
            _chip = Ui.Styled(string.Empty, "ha-chip");
            state.Add(_chip);
            Root.Add(state);
            var bar = Ui.Box("ha-col-bar");
            var track = Ui.Box("ha-bar");
            _fill = Ui.Box("ha-bar-fill");
            track.Add(_fill);
            bar.Add(track);
            _percent = Ui.Styled(string.Empty, "ha-bar-text");
            bar.Add(_percent);
            Root.Add(bar);
            _target = Ui.Styled(string.Empty, "ha-col-target");
            _distance = Ui.Styled(string.Empty, "ha-col-distance");
            _reason = Ui.Styled(string.Empty, "ha-col-reason");
            Root.Add(_target);
            Root.Add(_distance);
            Root.Add(_reason);
        }

        public HorrorEnemy Enemy { get; private set; }

        public Label NameLabel => _name;

        public Label Chip => _chip;

        public Label Reason => _reason;

        public void Bind(HorrorEnemy enemy)
        {
            Enemy = enemy;
            _state = -1;
            _suspicion = -1;
        }

        public void Update(string setup)
        {
            var enemy = Enemy;
            if (enemy == null)
            {
                Ui.SetText(_name, "(destroyed)");
                ShowIdle(Text.None, Text.None);
                return;
            }
            Ui.SetText(_name, Text.Name(enemy));
            if (!enemy.IsActive)
            {
                ShowIdle(enemy.archetype != null ? Text.Name(enemy.archetype) : "no archetype", setup ?? string.Empty);
                return;
            }
            ShowState((int)enemy.State);
            ShowSuspicion(enemy.Suspicion);
            var target = enemy.Target;
            Ui.SetText(_target, target != null ? Text.Name(target) : Text.None);
            Ui.SetText(_distance, target != null ? Text.Meters(Vector3.Distance(enemy.transform.position, target.transform.position)) : Text.None);
            Ui.SetText(_reason, enemy.LastTransition.Explanation);
        }

        #endregion


        #region Private and Protected

        private readonly Label _name;
        private readonly Label _chip;
        private readonly VisualElement _fill;
        private readonly Label _percent;
        private readonly Label _target;
        private readonly Label _distance;
        private readonly Label _reason;
        private string _chipClass;
        private string _fillClass;
        private int _state = -1;
        private int _suspicion = -1;

        private void ShowState(int state)
        {
            if (state == _state)
                return;
            _state = state;
            Ui.SetText(_chip, Ui.StateNames[state]);
            Ui.SwapClass(_chip, ref _chipClass, Ui.StateClasses[state]);
        }

        private void ShowSuspicion(float suspicion)
        {
            var percent = Mathf.Clamp(Mathf.RoundToInt(suspicion * 100f), 0, 100);
            if (percent == _suspicion)
                return;
            _suspicion = percent;
            _fill.style.width = Length.Percent(percent);
            Ui.SetText(_percent, Text.Percent(suspicion));
            Ui.SwapClass(_fill, ref _fillClass, percent >= 100 ? "ha-bar-fill--high" : null);
        }

        private void ShowIdle(string target, string reason)
        {
            if (_state != -2)
            {
                _state = -2;
                Ui.SetText(_chip, NotRunning);
                Ui.SwapClass(_chip, ref _chipClass, null);
            }
            if (_suspicion != -2)
            {
                _suspicion = -2;
                _fill.style.width = Length.Percent(0f);
                Ui.SetText(_percent, Text.None);
            }
            Ui.SetText(_target, target);
            Ui.SetText(_distance, Text.None);
            Ui.SetText(_reason, reason);
        }

        #endregion
    }
}
