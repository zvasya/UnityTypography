using System;
using System.Collections.Generic;
using System.Linq;
using Typography;
using Typography.OpenFont;
using Typography.OpenFont.Tables;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Typography_Editor
{
    [CustomEditor(typeof(UText))]
    public class UTextEditor : GraphicEditor
    {
        private SerializedProperty _font;
        private SerializedProperty _text;
        private SerializedProperty _fontSize;
        private SerializedProperty _alignment;
        private SerializedProperty _featureIndexList;

        private SuperFont _selectedFont;
        private Typeface _typeface;
        private List<FeatureInfo> _features;

        private class FeatureInfo
        {
            public string Tag;
            public ushort ID;
            public bool Enabled;
            public bool IsDefault;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _font = serializedObject.FindProperty("_font");
            _text = serializedObject.FindProperty("_text");
            _fontSize = serializedObject.FindProperty("_fontSize");
            _alignment = serializedObject.FindProperty("_alignment");
            _featureIndexList = serializedObject.FindProperty("_featureIndexList");
            ReloadFeaturesList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_font);
            if (EditorGUI.EndChangeCheck())
                ReloadFeaturesList();

            EditorGUILayout.PropertyField(_text);
            EditorGUILayout.PropertyField(_fontSize);
            EditorGUILayout.PropertyField(_alignment);
            
            FeaturesEditor();

            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }

        private void FeaturesEditor()
        {
            EditorGUI.BeginChangeCheck();
            foreach (var featureInfo in _features)
                featureInfo.Enabled = EditorGUILayout.Toggle(CreateLabel(featureInfo), featureInfo.Enabled);

            if (EditorGUI.EndChangeCheck())
                ApplyFeaturesListChange();
        }

        private static GUIContent CreateLabel(FeatureInfo featureInfo)
        {
            var id = featureInfo.ID;
            var tag = featureInfo.Tag;
            var defaultMark = featureInfo.IsDefault ? "✓" : "";
            var tooltip = featureInfo.IsDefault ? "This feature is enabled by default" : string.Empty;
            
            return new($"{id} {tag} {defaultMark}", tooltip);
        }

        private void ReloadFeaturesList()
        {
            if (_selectedFont != null)
                _featureIndexList.ClearArray();

            var curFeatures = CollectCurrentFeatures();

            _selectedFont = (SuperFont)_font.objectReferenceValue;
            _typeface = _selectedFont.LoadTypeface();

            var defaultFeatureList = GetDefaultFeatureList();

            _features = new List<FeatureInfo>();
            for (ushort i = 0; i < _typeface.GSUBTable.FeatureList.featureTables.Length; i++)
            {
                var feature = _typeface.GSUBTable.FeatureList.featureTables[i];
                _features.Add(
                    new FeatureInfo()
                    {
                        ID = i,
                        Tag = feature.TagName,
                        Enabled = curFeatures.Contains(i) || defaultFeatureList.Contains(i),
                        IsDefault = defaultFeatureList.Contains(i),
                    });
            }

            ApplyFeaturesListChange();
        }

        private ushort[] CollectCurrentFeatures()
        {
            if (_featureIndexList.arraySize == 0)
                return Array.Empty<ushort>();
            
            var curFeatures = new ushort[_featureIndexList.arraySize];
            for (var i = 0; i < _featureIndexList.arraySize; i++)
                curFeatures[i] = (ushort) _featureIndexList.GetArrayElementAtIndex(i).intValue;
            return curFeatures;
        }

        private void ApplyFeaturesListChange()
        {
            var featureInfos = _features.Where(f => f.Enabled).ToArray();
            _featureIndexList.arraySize = featureInfos.Length;
            for (var i = 0; i < featureInfos.Length; i++)
            {
                var feature = featureInfos[i];
                _featureIndexList.GetArrayElementAtIndex(i).intValue = feature.ID;
            }
        }

        private ushort[] GetDefaultFeatureList()
        {
            var scriptTag = ScriptTagDefs.Default.Tag;
            var langTag = 0U;
            
            var gsubTable = _typeface.GSUBTable;
            var scriptTable = gsubTable.ScriptList[scriptTag];

            if (scriptTable == null)
                return Array.Empty<ushort>();
            
            ScriptTable.LangSysTable selectedLang = null;
            
            if (langTag == 0)
            {
                //use default
                selectedLang = scriptTable.defaultLang;

                if (selectedLang == null && scriptTable.langSysTables != null &&
                    scriptTable.langSysTables.Length > 0)
                {
                    //some font not defult lang
                    //so we use it from langSysTable
                    //find selected lang,
                    //if not => choose default
                    selectedLang = scriptTable.langSysTables[0];
                }
            }
            else
            {
                if (langTag == scriptTable.defaultLang.langSysTagIden)
                {
                    //found
                    selectedLang = scriptTable.defaultLang;
                }

                if (scriptTable.langSysTables != null && scriptTable.langSysTables.Length > 0)
                {
                    //find selected lang,
                    //if not => choose default

                    foreach (var s in scriptTable.langSysTables)
                    {
                        if (s.langSysTagIden == langTag)
                        {
                            //found
                            selectedLang = s;
                            break;
                        }
                    }
                }
            }
            
            return selectedLang?.featureIndexList;
        }
    }
}