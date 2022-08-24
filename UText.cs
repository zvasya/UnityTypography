using System.Collections;
using System.Collections.Generic;
using DrawingGL;
using DrawingGL.Text;
using Typography;
using Typography.OpenFont;
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
    
    
    private TextPrinter _textPrinter;
    
    public void Init()
    {
        if (_textPrinter == null)
        {
            _textPrinter = new();
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
        var textRun = new TextRun();
        _textPrinter.FontSizeInPoints = 64;

        _textPrinter.GenerateGlyphRuns(textRun, _text.ToCharArray(), 0, _text.Length);

        return textRun;
    }

    float GetWidth()
    {
        var textRun = TextRun;
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
        Init();

        var textRun = TextRun;
        List<GlyphRun> glyphs = textRun._glyphs;
        int j = glyphs.Count;
        float accX = 0;
        float accY = 0;
        float nx = 0;
        float ny = 0;

        float pxscale = _textPrinter.Typeface.CalculateScaleToPixelFromPointSize(_textPrinter.FontSizeInPoints);

        int l = 0;
        for (int i = 0; i < j; ++i)
        {
            //render each glyph
            GlyphRun run = glyphs[i];

            UnscaledGlyphPlan plan = run.GlyphPlan;

            nx = accX + plan.OffsetX * pxscale;
            ny = accY + plan.OffsetY * pxscale;

            accX += (plan.AdvanceX * pxscale);

            if (run._tessData == null)
                continue;
            
            for (int k = 0; k < run._tessData.Length / 2; k++)
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

    protected override void OnValidate()
    {
        _textRun = null;
        base.OnValidate();
    }

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
    public float preferredHeight => 100;
    public float flexibleHeight => -1;
    public int layoutPriority => 0;
}
