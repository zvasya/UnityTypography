using System;
using System.Collections.Generic;
using System.Linq;
using DrawingGL;
using DrawingGL.Text;
using Typography.OpenFont;
using Typography.OpenFont.Extensions;
using Typography.TextLayout;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace Typography
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class UText3D : MonoBehaviour
    {
        [SerializeField] private SuperFont _font;
        [SerializeField] private string _text;
        [SerializeField] private int _fontSize;
        [SerializeField] private TextAnchor _alignment;
        [SerializeField] private ushort[] _featureIndexList;

        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private MeshFilter _meshFilter;

#if UNITY_EDITOR
        [NonSerialized] private int _prevFontSize;
        [NonSerialized] private SuperFont _prevFont;
        [NonSerialized] private ushort[] _prevFeatureIndexList;
#endif
        private TextPrinter _textPrinter;

        private void Awake()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshFilter == null)
                _meshFilter = GetComponent<MeshFilter>();
        }

        public void Init()
        {
            if (_textPrinter == null && _font != null)
            {
                _textPrinter = new(_featureIndexList);
                _textPrinter.EnableLigature = true;
                _textPrinter.PositionTechnique = PositionTechnique.OpenFont;
                Typeface tf = _font.LoadTypeface();
                _textPrinter.Typeface = tf;
            }
        }

        private TextRun _textRun;

        private TextRun TextRun => _textRun ??= CreateTextRun();

        private TextRun CreateTextRun()
        {
            Init();

            if (_textPrinter == null)
                return null;

            var textRun = new TextRun();
            _textPrinter.FontSizeInPoints = _fontSize * 0.75f; //TODO understand why it is so

            _textPrinter.GenerateGlyphRuns(textRun, _text.ToCharArray(), 0, _text.Length);

            return textRun;
        }

        float GetHeight()
        {
            return _textPrinter.Typeface.CalculateMaxLineClipHeight() *
                   _textPrinter.Typeface.CalculateScaleToPixelFromPointSize(_textPrinter.FontSizeInPoints);
        }

        float GetWidth()
        {
            var textRun = TextRun;

            if (textRun == null)
                return 0;

            List<GlyphRun> glyphs = textRun._glyphs;
            int j = glyphs.Count;
            float accX = 0;

            float pxscale = _textPrinter.Typeface.CalculateScaleToPixelFromPointSize(_textPrinter.FontSizeInPoints);

            for (var i = 0; i < j; ++i)
            {
                var run = glyphs[i];
                var plan = run.GlyphPlan;
                accX += (plan.AdvanceX * pxscale);
            }

            return accX;
        }

        void Rebuild()
        {
            Init();

            VertexHelper toFill = new VertexHelper();

            var textRun = TextRun;

            if (textRun == null)
                return;

            var glyphs = textRun._glyphs;
            var j = glyphs.Count;

            var pxScale = _textPrinter.Typeface.CalculateScaleToPixelFromPointSize(_textPrinter.FontSizeInPoints);
            var preferredWidth = GetWidth();
            var preferredHeight = GetHeight();

            var accX = _alignment switch
            {
                TextAnchor.LowerLeft or TextAnchor.MiddleLeft or TextAnchor.UpperLeft => 0f,
                TextAnchor.LowerRight or TextAnchor.MiddleRight or TextAnchor.UpperRight => -preferredWidth,
                _ => -preferredWidth / 2f,
            };
            var accY = _alignment switch
                       {
                           TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight => 0f,
                           TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight => -preferredHeight,
                           _ => -preferredHeight / 2f,
                       } -
                       (_textPrinter.Typeface.LineGap + _textPrinter.Typeface.Descender) * pxScale;

            var nx = 0f;
            var ny = 0f;

            var l = 0;
            for (var i = 0; i < j; ++i)
            {
                //render each glyph
                GlyphRun run = glyphs[i];

                UnscaledGlyphPlan plan = run.GlyphPlan;

                nx = accX + plan.OffsetX * pxScale;
                ny = accY + plan.OffsetY * pxScale;

                accX += (plan.AdvanceX * pxScale);

                if (run._tessData == null)
                    continue;

                for (var k = 0; k < run._tessData.Length / 2; k++)
                {
                    var h = k * 2;
                    toFill.AddVert(new Vector3(nx + run._tessData[h], ny + run._tessData[h + 1], 0), Color.white,
                        Vector4.zero);
                }

                var dir = (run._tessData[2] - run._tessData[0]) * (run._tessData[3] + run._tessData[1]) +
                          (run._tessData[4] - run._tessData[2]) * (run._tessData[5] + run._tessData[3]) +
                          (run._tessData[0] - run._tessData[4]) * (run._tessData[1] + run._tessData[5]) >
                          0;

                if (dir)
                    for (var k = 0; k < run._tessData.Length / 6; k++)
                        toFill.AddTriangle(l++, l++, l++);
                else
                    for (var k = 0; k < run._tessData.Length / 6; k++)
                    {
                        toFill.AddTriangle(l++, l + 1, l++);
                        l++;
                    }
            }

            var m = new Mesh();
            toFill.FillMesh(m);
            _meshFilter.mesh = m;
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_prevFont != _font ||
                _prevFontSize != _fontSize ||
                !_featureIndexList.SequenceEqual(_prevFeatureIndexList))
            {
                _prevFeatureIndexList = _featureIndexList.ToArray();
                _textPrinter = null;
                _prevFontSize = _fontSize;
                _prevFont = _font;
            }

            _textRun = null;
            Rebuild();
        }
#endif
    }
}