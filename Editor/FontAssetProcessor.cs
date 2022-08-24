using System;
using System.IO;
using System.Resources;
using Typography;
using Typography.OpenFont;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

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
            Typeface typeface;
            SuperFont superFont;
            using (FileStream fileStream = File.OpenRead(path))
            {
                superFont = ScriptableObject.CreateInstance<SuperFont>();
                superFont.bytes = new byte[fileStream.Length];
                fileStream.Read(superFont.bytes);
            }

            AssetDatabase.CreateAsset(superFont, Path.Combine(folder, name + ".asset"));
            AssetDatabase.SaveAssets();
        }
    }
    
}