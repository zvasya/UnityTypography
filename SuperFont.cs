using System.IO;
using Typography.OpenFont;
using UnityEngine;

namespace Typography
{
    public class SuperFont : ScriptableObject
    {
        [HideInInspector]
        public byte[] bytes;

        public Typeface LoadTypeface()
        {
            Stream s = new MemoryStream(bytes); 
            var reader = new OpenFontReader();
            var typeface = reader.Read(s);
            return typeface;
        }
    }
}