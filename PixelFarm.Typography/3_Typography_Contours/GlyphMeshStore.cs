﻿//MIT, 2016-present, WinterDev

using System;
using System.Collections.Generic;

using PixelFarm.Drawing;
using PixelFarm.Contours;
using PixelFarm.CpuBlit;

namespace Typography.OpenFont.Contours
{
    public struct GlyphControlParameters
    {
        public float avgXOffsetToFit;
        public short minX;
        public short minY;
        public short maxX;
        public short maxY;
    }

    public class GlyphMeshStore
    {

        class GlyphMeshData
        {
            public DynamicOutline dynamicOutline;
            public VertexStore vxsStore;
            public float avgXOffsetToFit;
            public Bounds orgBounds;
            public GlyphControlParameters GetControlPars()
            {
                var pars = new GlyphControlParameters();
                pars.minX = orgBounds.XMin;
                pars.minY = orgBounds.YMin;
                pars.maxX = orgBounds.XMax;
                pars.maxY = orgBounds.YMax;
                pars.avgXOffsetToFit = avgXOffsetToFit;
                return pars;
            }


            internal GlyphMeshData _synthOblique;

        }
        /// <summary>
        /// store typeface and its builder
        /// </summary>
        readonly Dictionary<Typeface, GlyphOutlineBuilder> _cacheGlyphOutlineBuilders = new Dictionary<Typeface, GlyphOutlineBuilder>();
        /// <summary>
        /// glyph mesh data for specific condition
        /// </summary>
        readonly GlyphMeshCollection<GlyphMeshData> _hintGlyphCollection = new GlyphMeshCollection<GlyphMeshData>();

        GlyphOutlineBuilder _currentGlyphBuilder;
        Typeface _currentTypeface;
        float _currentFontSizeInPoints;
        HintTechnique _currentHintTech;


        readonly GlyphTranslatorToVxs _tovxs = new GlyphTranslatorToVxs();

        static readonly AffineMat s_flipY;

        //shearing horizontal axis to right side, 20 degree, TODO: user can configure this value
        static readonly AffineMat s_slantHorizontal;

        static GlyphMeshStore()
        {

            s_flipY = AffineMat.Iden();
            s_flipY.Scale(1, -1);
            //
            s_slantHorizontal = AffineMat.Iden();
            s_slantHorizontal.Skew(AggMath.deg2rad(-15), 0);
        }
        public GlyphMeshStore()
        {

        }
        public void SetHintTechnique(HintTechnique hintTech)
        {
            _currentHintTech = hintTech;
        }

        /// <summary>
        /// simulate italic glyph
        /// </summary>
        public bool SimulateOblique { get; set; }

        public bool FlipGlyphUpward { get; set; }


        /// <summary>
        /// set current font
        /// </summary>
        /// <param name="typeface"></param>
        /// <param name="fontSizeInPoints"></param>
        public void SetFont(Typeface typeface, float fontSizeInPoints)
        {
            //temp fix,        
            if (_currentGlyphBuilder != null && !_cacheGlyphOutlineBuilders.ContainsKey(typeface))
            {
                //store current typeface to cache
                _cacheGlyphOutlineBuilders[_currentTypeface] = _currentGlyphBuilder;
            }
            _currentTypeface = typeface;
            _currentGlyphBuilder = null;
            if (typeface == null) return;

            //----------------------------
            //check if we have this in cache ?
            //if we don't have it, this _currentTypeface will set to null ***                  
            _cacheGlyphOutlineBuilders.TryGetValue(_currentTypeface, out _currentGlyphBuilder);
            if (_currentGlyphBuilder == null)
            {
                _currentGlyphBuilder = new GlyphOutlineBuilder(typeface);
            }
            //----------------------------------------------
            _currentFontSizeInPoints = fontSizeInPoints;

            //@prepare'note, 2017-10-20
            //temp fix, temp disable customfit if we build emoji font
            _currentGlyphBuilder.TemporaryDisableCustomFit = (typeface.COLRTable != null) && (typeface.CPALTable != null);
            //------------------------------------------ 
            _hintGlyphCollection.SetCacheInfo(typeface, _currentFontSizeInPoints, _currentHintTech);
        }

        /// <summary>
        /// always create new vxs for this glyph, no caching
        /// </summary>
        /// <param name="glyphIndex"></param>
        /// <returns></returns>
        public VertexStore GetNewUnFlattenVxs(ushort glyphIndex)
        {
            _currentGlyphBuilder.BuildFromGlyphIndex(glyphIndex, _currentFontSizeInPoints);
            DynamicOutline dynamicOutline = _currentGlyphBuilder.LatestGlyphFitOutline;

            //-----------------------------------  
            _tovxs.Reset();
            float pxscale = _currentTypeface.CalculateScaleToPixelFromPointSize(_currentFontSizeInPoints);

            if (dynamicOutline != null)
            {
                //TODO: review dynamic outline
                dynamicOutline.GenerateOutput(new ContourToGlyphTranslator(_tovxs), pxscale);
                //version 3 
                using (PixelFarm.CpuBlit.Tools.BorrowVxs(out var v1))
                {
                    _tovxs.WriteUnFlattenOutput(v1, 1);
                    return FlipGlyphUpward ? v1.CreateTrim(s_flipY) : v1.CreateTrim();
                }
            }
            else
            {
                using (PixelFarm.CpuBlit.Tools.BorrowVxs(out var v1))
                {
                    _currentGlyphBuilder.ReadShapes(_tovxs);
                    _tovxs.WriteUnFlattenOutput(v1, 1); //write to temp buffer first 

                    //then
                    return FlipGlyphUpward ? v1.CreateTrim(s_flipY) : v1.CreateTrim();
                }
            }

        }
        /// <summary>
        /// get existing or create new one from current font setting
        /// </summary>
        /// <param name="glyphIndex"></param>
        /// <returns></returns>
        GlyphMeshData InternalGetGlyphMesh(ushort glyphIndex)
        {
            if (!_hintGlyphCollection.TryGetCacheGlyph(glyphIndex, out GlyphMeshData glyphMeshData))
            {
                //if not found then create new glyph vxs and cache it
                _currentGlyphBuilder.SetHintTechnique(_currentHintTech);
                _currentGlyphBuilder.BuildFromGlyphIndex(glyphIndex, _currentFontSizeInPoints);
                DynamicOutline dynamicOutline = _currentGlyphBuilder.LatestGlyphFitOutline;
                //-----------------------------------  
                glyphMeshData = new GlyphMeshData();

                if (dynamicOutline != null)
                {
                    //has dynamic outline data
                    glyphMeshData.avgXOffsetToFit = dynamicOutline.AvgXFitOffset;
                    glyphMeshData.orgBounds = new Bounds(
                        (short)dynamicOutline.MinX, (short)dynamicOutline.MinY,
                        (short)dynamicOutline.MaxX, (short)dynamicOutline.MaxY);

                    glyphMeshData.dynamicOutline = dynamicOutline;
                }
                _hintGlyphCollection.RegisterCachedGlyph(glyphIndex, glyphMeshData);
                //-----------------------------------    
            }
            return glyphMeshData;
        }

        /// <summary>
        /// get glyph left offset-to-fit value from current font setting
        /// </summary>
        /// <param name="glyphIndex"></param>
        /// <returns></returns>
        public GlyphControlParameters GetControlPars(ushort glyphIndex) => InternalGetGlyphMesh(glyphIndex).GetControlPars();

        /// <summary>
        /// get glyph mesh from current font setting
        /// </summary>
        /// <param name="glyphIndex"></param>
        /// <returns></returns>
        public VertexStore GetGlyphMesh(ushort glyphIndex)
        {
            GlyphMeshData glyphMeshData = InternalGetGlyphMesh(glyphIndex);
            if (glyphMeshData.vxsStore == null)
            {
                //build vxs
                _tovxs.Reset();
                float pxscale = _currentTypeface.CalculateScaleToPixelFromPointSize(_currentFontSizeInPoints);
                DynamicOutline dynamicOutline = glyphMeshData.dynamicOutline;
                if (dynamicOutline != null)
                {
                    //TODO: review here aain
                    dynamicOutline.GenerateOutput(new ContourToGlyphTranslator(_tovxs), pxscale);
                    //version 3 
                    using (PixelFarm.CpuBlit.Tools.BorrowVxs(out var v1))
                    {
                        _tovxs.WriteOutput(v1);
                        glyphMeshData.vxsStore = FlipGlyphUpward ? v1.CreateTrim(s_flipY) : v1.CreateTrim();
                    }
                }
                else
                {
                    using (PixelFarm.CpuBlit.Tools.BorrowVxs(out var v1))
                    {
                        _currentGlyphBuilder.ReadShapes(_tovxs);
                        _tovxs.WriteOutput(v1); //write to temp buffer first 

                        //then
                        glyphMeshData.vxsStore = FlipGlyphUpward ? v1.CreateTrim(s_flipY) : v1.CreateTrim();
                    }
                }
            }


            if (SimulateOblique)
            {
                if (glyphMeshData._synthOblique == null)
                {
                    //create italic version
                    SimulateQbliqueGlyph(glyphMeshData);
                }
                return glyphMeshData._synthOblique.vxsStore;
            }
            else
            {
                return glyphMeshData.vxsStore;
            }
        }
        void SimulateQbliqueGlyph(GlyphMeshData orgGlyphMashData)
        {
            //_temp1.Clear();

            //PixelFarm.CpuBlit.VertexProcessing.VertexStoreTransformExtensions.TransformToVxs(_slantHorizontal, orgGlyphMashData.vxsStore, _temp1);
            //italic mesh data

            GlyphMeshData obliqueVersion = new GlyphMeshData();
            obliqueVersion.vxsStore = orgGlyphMashData.vxsStore.CreateTrim(s_slantHorizontal);

            orgGlyphMashData._synthOblique = obliqueVersion;

        }
    }


}