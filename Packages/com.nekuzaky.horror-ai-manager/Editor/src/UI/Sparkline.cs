using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.HorrorAI.Editor
{
    internal sealed class Sparkline : VisualElement
    {
        #region Public

        public Sparkline(float minimumScale, params string[] classes)
        {
            _minimumScale = minimumScale;
            AddToClassList("ha-spark");
            for (var i = 0; i < classes.Length; i++)
                AddToClassList(classes[i]);
            generateVisualContent += Draw;
        }

        public FloatRing Source { get; private set; }

        public void Show(FloatRing source)
        {
            if (ReferenceEquals(source, Source) && (source == null || source.Version == _version))
                return;
            Source = source;
            _version = source?.Version ?? -1;
            MarkDirtyRepaint();
        }

        #endregion


        #region Private and Protected

        private readonly float _minimumScale;
        private int _version = -1;

        private void Draw(MeshGenerationContext context)
        {
            var source = Source;
            var rect = contentRect;
            if (source == null || source.Count < 2 || rect.width < 4f || rect.height < 4f)
                return;
            var scale = Mathf.Max(_minimumScale, source.Max() * 1.15f);
            var color = resolvedStyle.color;
            var painter = context.painter2D;
            var step = rect.width / (source.Capacity - 1);
            var start = rect.width - step * (source.Count - 1);
            painter.fillColor = new Color(color.r, color.g, color.b, 0.16f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(start, rect.height));
            for (var i = 0; i < source.Count; i++)
                painter.LineTo(Point(source[i], i, start, step, scale, rect.height));
            painter.LineTo(new Vector2(start + step * (source.Count - 1), rect.height));
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = color;
            painter.lineWidth = 1.5f;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(Point(source[0], 0, start, step, scale, rect.height));
            for (var i = 1; i < source.Count; i++)
                painter.LineTo(Point(source[i], i, start, step, scale, rect.height));
            painter.Stroke();
        }

        private static Vector2 Point(float value, int index, float start, float step, float scale, float height) =>
            new(start + step * index, height - 2f - Mathf.Clamp01(value / scale) * (height - 4f));

        #endregion
    }
}
