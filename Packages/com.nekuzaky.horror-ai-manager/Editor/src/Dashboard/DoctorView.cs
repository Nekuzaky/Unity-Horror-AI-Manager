using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class DoctorView : IDashboardView
    {
        #region Public

        public VisualElement Root { get; }

        public DoctorView()
        {
            Root = Ui.Box("ha-view");
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("ha-scroll");
            var page = Ui.Box("ha-page");
            scroll.Add(page);
            Root.Add(scroll);
            page.Add(Ui.Styled("Checks the open scenes and fixes what it can in one click. Every fix can be undone.", "ha-help"));
            var toolbar = Ui.Box("ha-toolbar");
            toolbar.Add(Ui.TextButton(Glyphs.Refresh, "Check again", Run));
            _summary = Ui.Styled(string.Empty, "ha-card-label");
            toolbar.Add(_summary);
            page.Add(toolbar);
            _playNote = Ui.Styled("Play Mode: fixes made now are lost when you stop. Stop first to keep them.", "ha-help");
            page.Add(_playNote);
            _list = Ui.Box();
            page.Add(_list);
        }

        public IReadOnlyList<DoctorCheck> Checks => _checks;

        public void Run()
        {
            DoctorChecks.Run(_checks);
            _list.Clear();
            for (var i = 0; i < _checks.Count; i++)
                _list.Add(Card(_checks[i]));
            var errors = DoctorChecks.Count(_checks, CheckLevel.Error);
            var warnings = DoctorChecks.Count(_checks, CheckLevel.Warning);
            _summary.text = errors + warnings == 0 ? "Everything is ready." : errors + " to fix, " + warnings + " to check";
            _dirty = false;
        }

        public void Refresh(DashboardSampler sampler)
        {
            Ui.SetVisible(_playNote, Application.isPlaying);
            if (_dirty)
                Run();
        }

        public void SceneChanged() => _dirty = true;

        public void ProjectChanged() => _dirty = true;

        #endregion


        #region Private and Protected

        private readonly List<DoctorCheck> _checks = new(16);
        private readonly VisualElement _list;
        private readonly Label _summary;
        private readonly Label _playNote;
        private bool _dirty = true;

        private VisualElement Card(DoctorCheck check)
        {
            var card = Ui.Box("ha-check", LevelClass(check.Level));
            card.Add(Glyphs.Icon(LevelGlyph(check.Level), "ha-icon--large", LevelTint(check.Level)));
            var text = Ui.Box("ha-check-text");
            text.Add(Ui.Styled(check.Title, "ha-check-title"));
            text.Add(Ui.Styled(check.Detail, "ha-check-detail"));
            card.Add(text);
            if (check.Action == null)
                return card;
            var action = check.Action;
            card.Add(Ui.TextButton(Glyphs.Fix, check.Fix, () =>
            {
                action();
                Run();
            }));
            return card;
        }

        private static string LevelClass(CheckLevel level)
        {
            switch (level)
            {
                case CheckLevel.Error: return "ha-check--error";
                case CheckLevel.Warning: return "ha-check--warn";
                case CheckLevel.Info: return "ha-check--info";
                default: return "ha-check--ok";
            }
        }

        private static string LevelGlyph(CheckLevel level)
        {
            switch (level)
            {
                case CheckLevel.Error: return Glyphs.Error;
                case CheckLevel.Warning: return Glyphs.Warning;
                case CheckLevel.Info: return Glyphs.Info;
                default: return Glyphs.Ok;
            }
        }

        private static string LevelTint(CheckLevel level)
        {
            switch (level)
            {
                case CheckLevel.Error: return "ha-icon--bad";
                case CheckLevel.Warning: return "ha-icon--warn";
                case CheckLevel.Info: return "ha-icon--info";
                default: return "ha-icon--good";
            }
        }

        #endregion
    }
}
