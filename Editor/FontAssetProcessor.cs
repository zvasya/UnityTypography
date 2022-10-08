using System;
using System.IO;
using DrawingGL;
using DrawingGL.Text;
using PixelFarm.CpuBlit.VertexProcessing;
using Typography;
using Typography.Contours;
using Typography.OpenFont;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Vector2 = UnityEngine.Vector2;
using static VectorImageUtils.Editor.Utils;

public static class FontAssetProcessor
{
    [MenuItem("Assets/Create/Super Font", true)]
    public static bool CanProcessFont()
    {
        return Selection.activeObject is Font;       
    }
    
    [MenuItem("Assets/Create/Super Font")]
    public static void ProcessFont()
    {
        if (Selection.activeObject is Font font)
        {
            var path = AssetDatabase.GetAssetPath(font);
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);
            SuperFont superFont;
            using (FileStream fileStream = File.OpenRead(path))
            {
                superFont = ScriptableObject.CreateInstance<SuperFont>();
                superFont.bytes = new byte[fileStream.Length];
                superFont._glyphs = new();
                fileStream.Read(superFont.bytes);
            }

            AssetDatabase.CreateAsset(superFont, Path.Combine(folder, name + ".asset"));
            AssetDatabase.SaveAssets();

            Typeface typeface;
            using (FileStream fileStream = File.OpenRead(path))
            {
                var reader = new OpenFontReader();
                typeface = reader.Read(fileStream);
            }
            
            var _tessTool = new TessTool();
            var _pathTranslator = new GlyphTranslatorToPath();
            var _curveFlattener = new SimpleCurveFlattener();
            var _currentGlyphPathBuilder = new GlyphOutlineBuilder(typeface);
            // for (ushort i = 0; i < 100; i++)
            for (ushort i = 0; i < typeface.GlyphCount; i++)
            {
                var glyph = typeface.GetGlyph(i);

                VectorImage image = ScriptableObject.CreateInstance<VectorImage>();

                var writablePath = new WritablePath();
                _pathTranslator.SetOutput(writablePath);
                _currentGlyphPathBuilder.BuildFromGlyphIndex(i, 100);
                _currentGlyphPathBuilder.ReadShapes(_pathTranslator);

                float[] flattenPoints = _curveFlattener.Flatten(writablePath._points, out int[] endContours);
                float[] tessData = null;
                try
                {
                    tessData = _tessTool.TessAsTriVertexArray(flattenPoints, endContours, out int vertexCount);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error when building glyph with id = {i}:\n{e}");
                }

                if (tessData == null || tessData.Length < 1)
                    continue;

                var vert = new VectorImageVertexReplacer[tessData.Length / 2];
                // var m = Matrix4x4.TRS(new Vector3(0,-100,0),Quaternion.Euler(0, 0, 180), Vector3.one);
                for (var j = 0; j < tessData.Length; j+=2)
                {
                    var x = tessData[j];
                    var y = 100f - tessData[j + 1];
                    vert[j / 2] = new VectorImageVertexReplacer()
                    {
                        position = new Vector3(x,y,0),
                        tint = new Color32(255,255,255,255)
                    };
                }

                var dir = (tessData[2] - tessData[0]) * (tessData[3] + tessData[1]) +
                    (tessData[4] - tessData[2]) * (tessData[5] + tessData[3]) +
                    (tessData[0] - tessData[4]) * (tessData[1] + tessData[5]) > 0;
                
                ushort[] indexes = new ushort[tessData.Length / 2];
                for (ushort k = 0; k < tessData.Length / 6; k++)
                {
                    indexes[k*3 + 0] = (ushort)(k*3);
                    indexes[k*3 + 1] = dir ? (ushort)(k*3 + 1) : (ushort)(k*3 + 2);
                    indexes[k*3 + 2] = dir ? (ushort)(k*3 + 2) : (ushort)(k*3 + 1);
                }

                image.SetVertices(vert);
                image.SetIndexes(indexes);
                var underBaseLine = Mathf.Abs(typeface.Descender) + Mathf.Abs(typeface.LineGap);
                var lineHeight = typeface.Ascender + underBaseLine;
                
                var h = lineHeight * 100f / typeface.Ascender;

                var width2 = typeface.GetAdvanceWidthFromGlyphIndex(i) * h / typeface.UnitsPerEm;

                var size = new Vector2(width2,  h);
                image.SetSize(size);

                // image.
                image.name = i.ToString();
                AssetDatabase.AddObjectToAsset(image, superFont);
                superFont._glyphs.Add(new SuperFont.GlyphInfo(image, glyph.GlyphIndex, size));
            }
            
            AssetDatabase.SaveAssets();
        }
    }
}