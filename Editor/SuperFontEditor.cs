using System.Collections.Generic;
using Typography;
using Typography.OpenFont;
using Typography.OpenFont.Tables;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Typography_Editor
{
    [CustomEditor(typeof(SuperFont))]
    public class SuperFontEditor : Editor
    {
        private Typeface _typeface;
        private SuperFont _target;
        
        private Dictionary<ushort, SuperFont.GlyphInfo> _glyphs;

        private readonly StyleBackground _emptyBackground = new StyleBackground(StyleKeyword.None);
        

        private VisualTreeAsset _substitutionTableTree;
        private VisualTreeAsset SubstitutionTableTree => _substitutionTableTree ??= AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zvasya.unity_typography/Editor/Resources/SuperFontEditor_LkSubTableFromTo.uxml");

        private VisualTreeAsset _featureVisualTree;
        private VisualTreeAsset FeatureVisualTree => _featureVisualTree ??= AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zvasya.unity_typography/Editor/Resources/SuperFontEditor_Feature.uxml");
        
        public class Comparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                var result = x.CompareTo(y);
                return result == 0 ? 1 : result;
            }
        }
        
        private void Awake()
        {
            _target = target as SuperFont;
            _typeface = _target.LoadTypeface();
            
            FillGlyphs();
        }

        public override VisualElement CreateInspectorGUI()
        {
            base.CreateInspectorGUI();
            // Create a new VisualElement to be the root of our inspector UI
            VisualElement root = new VisualElement();
            if (_typeface == null)
                return root;

            var view = CreateSuperFontEditorRoot();
            root.Add(view);

            var content = root.Q<VisualElement>("Views");
            
            SortedList<int, VisualElement> features = new SortedList<int, VisualElement>(new Comparer());
            foreach (var feature in  _typeface.GSUBTable.FeatureList.featureTables)
            {
                foreach (ushort lookupIndex in feature.LookupListIndices)
                {
                    var list = _typeface.GSUBTable.LookupList[lookupIndex];
                    
                    VisualElement lookUpView = FeatureVisualTree.Instantiate();
                    lookUpView.Q<Foldout>("feature").text = $"{feature.TagName} lookup {list.dbugLkIndex.ToString()}";
                    var lookUpContent = lookUpView.Q<VisualElement>("Content");
                    for (var i = 0; i < list.SubTables.Length; i++)
                    {
                        GSUB.LookupSubTable lookupSubTable = list.SubTables[i];
                        var substitutionTable = lookupSubTable.GetSubtitutionTable();

                        switch (substitutionTable)
                        {
                            case string message:
                                lookUpContent.Add(CreateSubstitutionTableMessage(
                                    $"{feature.TagName} lookup {list.dbugLkIndex.ToString()} subtable {i}", message));
                                break;
                            case GSUB.SubtitutionTableOneToOne table:
                                lookUpContent.Add(CreateSubstitutionTableOneToOne(
                                    $"{feature.TagName} lookup {list.dbugLkIndex.ToString()} subtable {i}", table));
                                break;
                            case GSUB.SubtitutionTableManyToOne table:
                                lookUpContent.Add(CreateSubtitutionTableManyToOne(
                                    $"{feature.TagName} lookup {list.dbugLkIndex.ToString()} subtable {i}", table));
                                break;
                        }
                    }

                    features.Add(list.dbugLkIndex, lookUpView);
                }
            }

            foreach (var (_,visualElement) in features)
            {
                content.Add(visualElement);
            }
            
            return root;
        }

        private static VisualElement CreateSuperFontEditorRoot()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zvasya.unity_typography/Editor/Resources/SuperFontEditor.uxml");
            return visualTree.Instantiate();
        }


        private VisualElement CreateSubstitutionContent(string title, out VisualElement content)
        {
            VisualElement subTableView = FeatureVisualTree.Instantiate();
            subTableView.Q<Foldout>("feature").text = title;
            content = subTableView.Q<VisualElement>("Content");
            return subTableView;
        }

        private VisualElement CreateSubstitutionTableMessage(string title, string message)
        {
            var view = CreateSubstitutionContent(title, out var content);
            content.Add(new Label(message));
            return view;
        }

        private VisualElement CreateSubstitutionTableOneToOne(string title, GSUB.SubtitutionTableOneToOne table)
        {
            var view = CreateSubstitutionContent(title, out var content);
            var elementTree = SubstitutionTableTree;
            foreach (var (source, destination) in table)
            {
                VisualElement elementView = elementTree.Instantiate();
                InsertGlyph(elementView.Q<VisualElement>("from"),  source);
                InsertGlyph(elementView.Q<VisualElement>("to"),  destination);
                content.Add(elementView);
            }
            return view;
        }
        
        private VisualElement CreateSubtitutionTableManyToOne(string title, GSUB.SubtitutionTableManyToOne table)
        {
            var view = CreateSubstitutionContent(title, out var content);
            var elementTree = SubstitutionTableTree;
            foreach (var (source, destination) in table)
            {
                VisualElement elementView = elementTree.Instantiate();
                InsertGlyphs(elementView.Q<VisualElement>("from"), source);
                InsertGlyph(elementView.Q<VisualElement>("to"), destination);
                content.Add(elementView);
            }
            return view;
        }

        private void InsertGlyph(VisualElement parent, ushort glyph)
        {
            var newEnt = CreateGlyphView(glyph);
            parent.Add(newEnt);
        }
        private void InsertGlyphs(VisualElement parent, List<ushort> glyphs)
        {
            for (var i = 0; i < glyphs.Count; i++)
            {
                if (i != 0)
                {
                    parent.Add(new Label("+"));
                }

                var glyph = glyphs[i];
                var newEnt = CreateGlyphView(glyph);
                parent.Add(newEnt);
            }
        }
        
        private VisualElement CreateGlyphView(ushort ff)
        {
            var bgi = StyleBackgroundImage(ff, out var glyph);

            VisualElement newEnt = new VisualElement();
            var width = glyph.Size.x / 8f;
            if (width < 1)
                width = 10;
            newEnt.style.width = width;
            newEnt.style.height = glyph.Size.y / 8f;
            newEnt.style.unityBackgroundScaleMode = new(ScaleMode.ScaleToFit);
            newEnt.style.backgroundImage = bgi;
            return newEnt;
        }

        private void FillGlyphs()
        {
            _glyphs = new Dictionary<ushort, SuperFont.GlyphInfo>();
            for (int i = 0; i < _target._glyphs.Count; i++)
            {
                _glyphs.Add(_target._glyphs[i].Index, _target._glyphs[i]);
            }
        }

        private StyleBackground StyleBackgroundImage(ushort glyphID, out SuperFont.GlyphInfo glyph)
        {
            return _glyphs.TryGetValue(glyphID, out glyph) ? new StyleBackground(glyph.Image) : _emptyBackground;
        }
    }
}