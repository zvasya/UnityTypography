﻿//Apache2, 2016-2017,  WinterDev


namespace Typography.OpenFont.Tables
{
    /// <summary>
    /// replaceable glyph index list
    /// </summary>
    public class GlyphIndexList
    {
        System.Collections.Generic.List<ushort> _glyphIndices = new System.Collections.Generic.List<ushort>();
        public void Clear()
        {
            _glyphIndices.Clear();
        }
        public void AddGlyphIndex(ushort glyphIndex)
        {
            _glyphIndices.Add(glyphIndex);
        }
        public int Count { get { return _glyphIndices.Count; } }
        public ushort this[int index] { get { return _glyphIndices[index]; } }

        /// <summary>
        /// remove:add_new 1:1
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newGlyphIndex"></param>
        public void Replace(int index, ushort newGlyphIndex)
        {
            _glyphIndices[index] = newGlyphIndex;
        }
        /// <summary>
        /// remove:add_new >=1:1
        /// </summary>
        /// <param name="index"></param>
        /// <param name="removeLen"></param>
        /// <param name="newGlyhIndex"></param>
        public void Replace(int index, int removeLen, ushort newGlyhIndex)
        {
            _glyphIndices.RemoveRange(index, removeLen);
            _glyphIndices.Insert(index, newGlyhIndex);
        }
        /// <summary>
        /// remove: add_new 1:>=1
        /// </summary>
        /// <param name="index"></param>
        /// <param name="removeLen"></param>
        /// <param name="newGlyhIndex"></param>
        public void Replace(int index, ushort[] newGlyhIndices)
        {
            _glyphIndices.RemoveAt(index);
            _glyphIndices.InsertRange(index, newGlyhIndices);
        }
    }

    partial class GSUB : TableEntry
    {
        /// <summary>
        /// base class of lookup sub table
        /// </summary>
        public abstract class LookupSubTable
        {
            public abstract void DoSubtitution(GlyphIndexList glyphIndices, int startAt, int len);
            public GSUB OwnerGSub;
        }
    }
}