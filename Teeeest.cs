using System;
using System.Collections.Generic;
using System.IO;
using DrawingGL;
using DrawingGL.Text;
using Typography.OpenFont;
using Typography.TextLayout;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace Typography
{
    [ExecuteAlways]
    public class Teeeest : MonoBehaviour
    {
        [SerializeField] private SuperFont font;

        [SerializeField] private MeshRenderer mr;
        [SerializeField] private MeshFilter mf;
        
        private TextPrinter _textPrinter;
        [SerializeField] private string _text;

        public void Init()
        {
            if (_textPrinter == null && font != null)
            {
                _textPrinter = new();
                _textPrinter.EnableLigature = true;
                _textPrinter.PositionTechnique = PositionTechnique.OpenFont;
                Typeface tf = font.LoadTypeface();
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
            _textPrinter.FontSizeInPoints = 64;

            _textPrinter.GenerateGlyphRuns(textRun, _text.ToCharArray(), 0, _text.Length);

            return textRun;
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
            var textRun = TextRun;
            
            if (textRun == null)
                return;
            
            List<GlyphRun> glyphs = textRun._glyphs;
            int j = glyphs.Count;
            float accX = -GetWidth()/2;
            float accY = 0;
            float nx = 0;
            float ny = 0;

            float pxscale = _textPrinter.Typeface.CalculateScaleToPixelFromPointSize(_textPrinter.FontSizeInPoints);

            VertexHelper vh = new VertexHelper();

            int l = 0;
            for (int i = 0; i < j; ++i)
            {
                //render each glyph
                GlyphRun run = glyphs[i];

                TextLayout.UnscaledGlyphPlan plan = run.GlyphPlan;

                nx = accX + plan.OffsetX * pxscale;
                ny = accY + plan.OffsetY * pxscale;

                accX += (plan.AdvanceX * pxscale);

                if (run._tessData == null)
                    continue;
                
                for (int k = 0; k < run._tessData.Length / 2; k++)
                {
                    var h = k * 2;
                    vh.AddVert(new Vector3(nx + run._tessData[h], ny + run._tessData[h + 1], 0), Color.white,
                        Vector4.zero);
                }

                for (int k = 0; k < run._tessData.Length / 6; k++)
                {
                    vh.AddTriangle(l++, l++, l++);
                }
            }

            Mesh m = new Mesh();
            vh.FillMesh(m);
            mf.mesh = m;
        }

        private void OnValidate()
        {
            _textRun = null;
            Rebuild();
        }
    }
}