using System;
using System.Collections.Generic;
using System.IO;
using Typography.OpenFont;
using UnityEngine;
using UnityEngine.UIElements;

namespace Typography
{
    public class SuperFont : ScriptableObject
    {
        [Serializable]
        public struct GlyphInfo
        {
            [SerializeField] private VectorImage _image;
            [SerializeField] private ushort _index;
            [SerializeField] private Vector2 _size;
            
            public VectorImage Image => _image;
            public ushort Index => _index;
            public Vector2 Size => _size;

            public GlyphInfo(VectorImage image, ushort index, Vector2 size)
            {
                _image = image;
                _index = index;
                _size = size;
            }
            
        }
        [HideInInspector]
        public byte[] bytes;
        
        [HideInInspector]
        public List<GlyphInfo> _glyphs;

        public Typeface LoadTypeface()
        {
            Stream s = new MemoryStream(bytes); 
            var reader = new OpenFontReader();
            var typeface = reader.Read(s);
            return typeface;
        }
    }
}