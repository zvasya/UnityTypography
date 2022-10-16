using System;
using System.Collections.Generic;
using System.Linq;
using DrawingGL;
using DrawingGL.Text;
using Typography;
using Typography.OpenFont;
using Typography.OpenFont.Extensions;
using Typography.TextLayout;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

[RequireComponent(typeof(CanvasRenderer))]
[AddComponentMenu("UI/UText", 101)]
public class UText : MaskableGraphic, ILayoutElement
{
    [SerializeField] private SuperFont _font;
    [SerializeField] private string _text;
    [SerializeField] private int _fontSize;
    [SerializeField] private TextAnchor _alignment;

#if UNITY_EDITOR
    [NonSerialized] private int _prevFontSize;
    [NonSerialized] private SuperFont _prevFont;
    [NonSerialized] private ushort[] _prevFeatureIndexList;
#endif
    private TextPrinter _textPrinter;

    [SerializeField] private ushort[] _featureIndexList;
    
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

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        toFill.Clear();

        var textRun = TextRun;
        if (textRun == null)
            return;

        var glyphs = textRun._glyphs;
        var j = glyphs.Count;
        var rect = rectTransform.rect;
        var pxScale = _textPrinter.Typeface.CalculateScaleToPixelFromPointSize(_textPrinter.FontSizeInPoints);
        
        var accX = _alignment switch
        {
            TextAnchor.LowerLeft or TextAnchor.MiddleLeft or TextAnchor.UpperLeft => rect.xMin,
            TextAnchor.LowerRight or TextAnchor.MiddleRight or TextAnchor.UpperRight => rect.xMax - preferredWidth,
            _ => rect.center.x - preferredWidth / 2,
        };
        var accY = _alignment switch
        {
            TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight => rect.yMin,
            TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight => rect.yMax - preferredHeight,
            _ => rect.center.y - preferredHeight / 2,
        } - (_textPrinter.Typeface.LineGap + _textPrinter.Typeface.Descender) * pxScale;
        
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

            for (int k = 0; k < run._tessData.Length / 6; k++)
            {
                toFill.AddTriangle(l++, l++, l++);
            }
        }
    }

#if UNITY_EDITOR    
    protected override void OnValidate()
    {
        if (_prevFont != _font || _prevFontSize != _fontSize || !_featureIndexList.SequenceEqual(_prevFeatureIndexList))
        {
            _prevFeatureIndexList = _featureIndexList.ToArray();
            _textPrinter = null;
            _prevFontSize = _fontSize;
            _prevFont = _font;
        }

        _textRun = null;
        base.OnValidate();
    }
#endif

    public void CalculateLayoutInputHorizontal()
    {
    }

    public void CalculateLayoutInputVertical()
    {
    }

    public float minWidth => 0;
    public float preferredWidth => GetWidth();
    public virtual float flexibleWidth => -1;
    public float minHeight => 0;
    public float preferredHeight => GetHeight();
    public float flexibleHeight => -1;
    public int layoutPriority => 0;
}