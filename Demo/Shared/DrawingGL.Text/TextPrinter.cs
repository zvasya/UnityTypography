﻿//MIT, 2017, Zou Wei(github/zwcloud), WinterDev
using PixelFarm.CpuBlit.VertexProcessing;
using Typography.OpenFont;
using Typography.TextLayout;
using Typography.Contours;

namespace DrawingGL.Text
{
    /// <summary>
    /// text printer
    /// </summary>
    class TextPrinter : TextPrinterBase
    {
        //funcs:
        //1. layout glyph
        //2. measure glyph
        //3. generate glyph runs into textrun 
        GlyphTranslatorToPath _pathTranslator;
        GlyphOutlineBuilder _currentGlyphPathBuilder;

        //
        // for tess
        // 
        readonly SimpleCurveFlattener _curveFlattener;
        readonly TessTool _tessTool;

        Typeface _currentTypeface;

        //-------------
        struct ProcessedGlyph
        {
            public readonly float[] tessData;
            public readonly ushort vertextCount;
            public ProcessedGlyph(float[] tessData, ushort vertextCount)
            {
                this.tessData = tessData;
                this.vertextCount = vertextCount;
            }
        }
        GlyphMeshCollection<ProcessedGlyph> _glyphMeshCollection = new GlyphMeshCollection<ProcessedGlyph>();
        //-------------
        public TextPrinter(ushort[] featureIndexList)
        {
            FontSizeInPoints = 14;
            
            ScriptLang = new ScriptLang("latn");

            //
            _curveFlattener = new SimpleCurveFlattener();

            _tessTool = new TessTool();
            GlyphLayoutMan = new GlyphLayout(featureIndexList);
        }


        public override void DrawFromGlyphPlans(GlyphPlanSequence glyphPlanList, int startAt, int len, float x, float y)
        {
            throw new System.NotImplementedException();
        }
        public override GlyphLayout GlyphLayoutMan { get; }

        public override Typeface Typeface
        {
            get => _currentTypeface;
            set
            {
                _currentTypeface = value;
                GlyphLayoutMan.Typeface = value;

                    //2. glyph builder
                    _currentGlyphPathBuilder = new GlyphOutlineBuilder(Typeface);
                    _currentGlyphPathBuilder.UseTrueTypeInstructions = false; //reset
                    _currentGlyphPathBuilder.UseVerticalHinting = false; //reset
                    switch (this.HintTechnique)
                    {
                        case HintTechnique.TrueTypeInstruction:
                            _currentGlyphPathBuilder.UseTrueTypeInstructions = true;
                            break;
                        case HintTechnique.TrueTypeInstruction_VerticalOnly:
                            _currentGlyphPathBuilder.UseTrueTypeInstructions = true;
                            _currentGlyphPathBuilder.UseVerticalHinting = true;
                            break;
                        case HintTechnique.CustomAutoFit:
                            //custom agg autofit 
                            break;
                    }

                    //3. glyph translater
                    _pathTranslator = new GlyphTranslatorToPath();

                    //4. Update GlyphLayout
                    GlyphLayoutMan.ScriptLang = this.ScriptLang;
                    GlyphLayoutMan.PositionTechnique = this.PositionTechnique;
                    GlyphLayoutMan.EnableLigature = this.EnableLigature;
                }
            }
        public MeasuredStringBox Measure(char[] textBuffer, int startAt, int len)
        {
            return GlyphLayoutMan.LayoutAndMeasureString(
                textBuffer, startAt, len,
                this.FontSizeInPoints
                );
        }


        UnscaledGlyphPlanList _resuableGlyphPlanList = new UnscaledGlyphPlanList();

        /// <summary>
        /// generate glyph run into a given textRun
        /// </summary>
        /// <param name="outputTextRun"></param>
        /// <param name="charBuffer"></param>
        /// <param name="start"></param>
        /// <param name="len"></param>
        public void GenerateGlyphRuns(TextRun outputTextRun, char[] charBuffer, int start, int len)
        {
            // layout glyphs with selected layout technique
            float sizeInPoints = this.FontSizeInPoints;
            outputTextRun.typeface = this.Typeface;
            outputTextRun.sizeInPoints = sizeInPoints;

            //in this version we store original glyph into the mesh collection
            //and then we scale it later, so I just specific font size=0 (you can use any value)
            _glyphMeshCollection.SetCacheInfo(this.Typeface, 0, this.HintTechnique);


            GlyphLayoutMan.Typeface = this.Typeface;
            GlyphLayoutMan.Layout(charBuffer, start, len);

            float pxscale = this.Typeface.CalculateScaleToPixelFromPointSize(sizeInPoints);

            _resuableGlyphPlanList.Clear();
            GenerateGlyphPlan(charBuffer, 0, charBuffer.Length, _resuableGlyphPlanList);

            // render each glyph 
            int planCount = _resuableGlyphPlanList.Count;
            for (var i = 0; i < planCount; ++i)
            {

                _pathTranslator.Reset();
                //----
                //glyph path 
                //---- 
                UnscaledGlyphPlan glyphPlan = _resuableGlyphPlanList[i];
                //
                //1. check if we have this glyph in cache?
                //if yes, not need to build it again 



                if (!_glyphMeshCollection.TryGetCacheGlyph(glyphPlan.glyphIndex, out ProcessedGlyph processGlyph))
                {
                    //if not found the  create a new one and register it
                    var writablePath = new WritablePath();
                    _pathTranslator.SetOutput(writablePath);
                    _currentGlyphPathBuilder.BuildFromGlyphIndex(glyphPlan.glyphIndex, sizeInPoints);
                    _currentGlyphPathBuilder.ReadShapes(_pathTranslator);

                    //-------
                    //do tess   
                    float[] flattenPoints = _curveFlattener.Flatten(writablePath._points, out int[] endContours);

                    float[] tessData = _tessTool.TessAsTriVertexArray(flattenPoints, endContours, out int vertexCount);
                    processGlyph = new ProcessedGlyph(tessData, (ushort)vertexCount);

                    _glyphMeshCollection.RegisterCachedGlyph(glyphPlan.glyphIndex, processGlyph);
                }

                outputTextRun.AddGlyph(
                    new GlyphRun(glyphPlan,
                        processGlyph.tessData,
                        processGlyph.vertextCount));
            }
        }
        public override void DrawString(char[] textBuffer, int startAt, int len, float x, float y)
        {

        }

    }


}