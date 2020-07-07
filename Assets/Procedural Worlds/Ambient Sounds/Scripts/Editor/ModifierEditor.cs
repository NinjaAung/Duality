// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using PWCommon2;
using AmbientSounds.Internal;

/*
 * Custom Editor for Modifier Components
 */

namespace AmbientSounds {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Modifier))]
    public class ModifierEditor : PWEditor, IPWEditor {
        /// <summary> Reference to EditorUtils for GUI functions </summary>
        private EditorUtils m_editorUtils;

        //Property references for Modifier object
        SerializedProperty m_requirements;
        SerializedProperty m_valuesMix;
        SerializedProperty m_values;
        SerializedProperty m_eventsMix;
        SerializedProperty m_events;
        SerializedProperty m_modVolume;
        SerializedProperty m_volume;
        SerializedProperty m_modPlaybackSpeed;
        SerializedProperty m_playbackSpeed;
        SerializedProperty m_modClips;
        SerializedProperty m_modClipsType;
        SerializedProperty m_clipData;
        SerializedProperty m_modDelayChance;
        SerializedProperty m_delayChance;
        SerializedProperty m_modDelay;
        SerializedProperty m_minMaxDelay;
        SerializedProperty m_modRandomizeVolume;
        SerializedProperty m_randomizeVolume;
        SerializedProperty m_modMinMaxVolume;
        SerializedProperty m_minMaxVolume;
        SerializedProperty m_modRandomizePlaybackSpeed;
        SerializedProperty m_randomizePlaybackSpeed;
        SerializedProperty m_modMinMaxPlaybackSpeed;
        SerializedProperty m_minMaxPlaybackSpeed;

        /// <summary> Animated Boolean for if values should be displayed </summary>
        AnimBool valuesGroup;
        /// <summary> Aniamted Boolean for if Events should be displayed </summary>
        AnimBool eventsGroup;
        /// <summary> Animated Boolean for if Clips modification list should be displayed </summary>
        AnimBool clipsVisible = null;

        bool m_clipsExpanded = true;
        UnityEditorInternal.ReorderableList m_eventsReorderable;
        UnityEditorInternal.ReorderableList m_valuesReorderable;
        UnityEditorInternal.ReorderableList m_clipsReorderable;

        #region Custom Fields
        /// <summary> Draws a standard property with a "toggle" to the left of label to disable the control </summary>
        /// <param name="position">Rect to draw property in</param>
        /// <param name="toggleVal">Property to use for "toggle" function</param>
        /// <param name="Val">Main Property to display</param>
        /// <param name="label">GUI Content to display as label</param>
        void ToggleField(Rect position, SerializedProperty toggleVal, SerializedProperty Val, GUIContent label = null) {
            position = EditorGUI.IndentedRect(position);
            Rect toggleRect = new Rect(position.x, position.y, position.height, position.height);
            bool toggleEnabled = false;
            switch (toggleVal.propertyType) {
                case SerializedPropertyType.Float:
                    toggleEnabled = toggleVal.floatValue != 0f;
                    break;
                case SerializedPropertyType.Integer:
                    toggleEnabled = toggleVal.intValue != 0;
                    break;
                case SerializedPropertyType.Boolean:
                    toggleEnabled = toggleVal.boolValue;
                    break;
                default:
                    toggleEnabled = true;
                    Debug.LogWarning("Invalid ToggleValue passed. Expected Boolean, Integer, or Float. Got " + toggleVal.type);
                    break;
            }
            toggleEnabled = GUI.Toggle(toggleRect, toggleEnabled, "");
            switch (toggleVal.propertyType) {
                case SerializedPropertyType.Float:
                    toggleVal.floatValue = toggleEnabled ? 1f : 0f;
                    break;
                case SerializedPropertyType.Integer:
                    toggleVal.intValue = toggleEnabled ? 1 : 0;
                    break;
                case SerializedPropertyType.Boolean:
                    toggleVal.boolValue = toggleEnabled;
                    break;
            }
            position.x += toggleRect.width;
            position.width -= toggleRect.width;
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.BeginDisabledGroup(!toggleEnabled);
            if (label == null)
                EditorGUI.PropertyField(position, Val);
            else
                EditorGUI.PropertyField(position, Val, label);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel = oldIndent;
        }
        /// <summary> Draws a Vector2 property with a "toggle" to the left of label to disable the control </summary>
        /// <param name="position">Rect to draw property in</param>
        /// <param name="toggleVal">Property to use for "toggle" function</param>
        /// <param name="Val">Main Property to display</param>
        /// <param name="label">GUI Content to display as main label</param>
        /// <param name="subLabel1">GUI Content to display for label of first field</param>
        /// <param name="subLabel2">GUI Content to display for label of second field</param>
        void ToggleVector2Field(Rect position, SerializedProperty toggleVal, SerializedProperty Val, GUIContent label, GUIContent subLabel1, GUIContent subLabel2) {
            position = EditorGUI.IndentedRect(position);
            Rect toggleRect = new Rect(position.x, position.y, position.height, position.height);
            bool toggleEnabled = false;
            switch (toggleVal.propertyType) {
                case SerializedPropertyType.Float:
                    toggleEnabled = toggleVal.floatValue != 0f;
                    break;
                case SerializedPropertyType.Integer:
                    toggleEnabled = toggleVal.intValue != 0;
                    break;
                case SerializedPropertyType.Boolean:
                    toggleEnabled = toggleVal.boolValue;
                    break;
                default:
                    toggleEnabled = true;
                    Debug.LogWarning("Invalid ToggleValue passed. Expected Boolean, Integer, or Float. Got " + toggleVal.type);
                    break;
            }
            toggleEnabled = GUI.Toggle(toggleRect, toggleEnabled, "");
            switch (toggleVal.propertyType) {
                case SerializedPropertyType.Float:
                    toggleVal.floatValue = toggleEnabled ? 1f : 0f;
                    break;
                case SerializedPropertyType.Integer:
                    toggleVal.intValue = toggleEnabled ? 1 : 0;
                    break;
                case SerializedPropertyType.Boolean:
                    toggleVal.boolValue = toggleEnabled;
                    break;
            }
            position.x += toggleRect.width;
            position.width -= toggleRect.width;
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.BeginDisabledGroup(!toggleEnabled);

            float[] vals = new float[2] { Val.vector2Value.x, Val.vector2Value.y };
            EditorGUI.BeginChangeCheck();
            position = EditorGUI.PrefixLabel(position, label);
            position.width = position.width / 2f;
            Rect fieldRect = position;
            if (!string.IsNullOrEmpty(subLabel1.text)) {
                Rect labelRect = position;
                labelRect.width = EditorStyles.miniLabel.CalcSize(subLabel1).x;
                GUI.Label(labelRect, subLabel1, EditorStyles.miniLabel);
                fieldRect.xMin += labelRect.width;
            }
            vals[0] = EditorGUI.FloatField(fieldRect, "", vals[0]);
            position.x += position.width;
            fieldRect = position;
            if (!string.IsNullOrEmpty(subLabel2.text)) {
                Rect labelRect = position;
                labelRect.width = EditorStyles.miniLabel.CalcSize(subLabel2).x;
                GUI.Label(labelRect, subLabel2, EditorStyles.miniLabel);
                fieldRect.xMin += labelRect.width;
            }
            vals[1] = EditorGUI.FloatField(fieldRect, "", vals[1]);
            //EditorGUI.MultiFloatField(position, label, new GUIContent[] { subLabel1, subLabel2 }, vals);
            if (EditorGUI.EndChangeCheck())
                Val.vector2Value = new Vector2(vals[0], vals[1]);

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel = oldIndent;
        }
        /// <summary> Draws a SliderRange property with a "toggle" to the left of label to disable the control </summary>
        /// <param name="position">Rect to draw property in</param>
        /// <param name="toggleVal">Property to use for "toggle" function</param>
        /// <param name="Val">Main Property to display</param>
        /// <param name="label">GUI Content to display as label</param>
        void ToggleSliderRangeField(Rect position, SerializedProperty toggleVal, SerializedProperty Val, GUIContent label = null, float minVal = 0f, float maxVal = 1f) {
            position = EditorGUI.IndentedRect(position);
            Rect toggleRect = new Rect(position.x, position.y, position.height, position.height);
            bool toggleEnabled = false;
            switch (toggleVal.propertyType) {
                case SerializedPropertyType.Float:
                    toggleEnabled = toggleVal.floatValue != 0f;
                    break;
                case SerializedPropertyType.Integer:
                    toggleEnabled = toggleVal.intValue != 0;
                    break;
                case SerializedPropertyType.Boolean:
                    toggleEnabled = toggleVal.boolValue;
                    break;
                default:
                    toggleEnabled = true;
                    Debug.LogWarning("Invalid ToggleValue passed. Expected Boolean, Integer, or Float. Got " + toggleVal.type);
                    break;
            }
            toggleEnabled = GUI.Toggle(toggleRect, toggleEnabled, "");
            switch (toggleVal.propertyType) {
                case SerializedPropertyType.Float:
                    toggleVal.floatValue = toggleEnabled ? 1f : 0f;
                    break;
                case SerializedPropertyType.Integer:
                    toggleVal.intValue = toggleEnabled ? 1 : 0;
                    break;
                case SerializedPropertyType.Boolean:
                    toggleVal.boolValue = toggleEnabled;
                    break;
            }
            position.x += toggleRect.width;
            position.width -= toggleRect.width;
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.BeginDisabledGroup(!toggleEnabled);
            if (label == null)
                m_editorUtils.SliderRange(position, Val, new Vector2(minVal, maxVal));
            else
                m_editorUtils.SliderRange(position, label, Val, new Vector2(minVal, maxVal));
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel = oldIndent;
        }
        #endregion
        /// <summary> Destructor to release references </summary>
        private void OnDestroy() {
            if (m_editorUtils != null) {
                m_editorUtils.Dispose();
            }
        }
        /// <summary> Constructor to set up references </summary>
        public void OnEnable() {
            m_requirements = serializedObject.FindProperty("m_requirements");
            m_valuesMix = serializedObject.FindProperty("m_valuesMix");
            m_values = serializedObject.FindProperty("m_values");
            m_eventsMix = serializedObject.FindProperty("m_eventsMix");
            m_events = serializedObject.FindProperty("m_events");
            m_modVolume = serializedObject.FindProperty("m_modVolume");
            m_volume = serializedObject.FindProperty("m_volume");
            m_modPlaybackSpeed = serializedObject.FindProperty("m_modPlaybackSpeed");
            m_playbackSpeed = serializedObject.FindProperty("m_playbackSpeed");
            m_modClips = serializedObject.FindProperty("m_modClips");
            m_modClipsType = serializedObject.FindProperty("m_modClipsType");
            m_clipData = serializedObject.FindProperty("m_clipData");
            m_modDelayChance = serializedObject.FindProperty("m_modDelayChance");
            m_delayChance = serializedObject.FindProperty("m_delayChance");
            m_modDelay = serializedObject.FindProperty("m_modDelay");
            m_minMaxDelay = serializedObject.FindProperty("m_minMaxDelay");
            m_modRandomizeVolume = serializedObject.FindProperty("m_modRandomizeVolume");
            m_randomizeVolume = serializedObject.FindProperty("m_randomizeVolume");
            m_modMinMaxVolume = serializedObject.FindProperty("m_modMinMaxVolume");
            m_minMaxVolume = serializedObject.FindProperty("m_minMaxVolume");
            m_modRandomizePlaybackSpeed = serializedObject.FindProperty("m_modRandomizePlaybackSpeed");
            m_randomizePlaybackSpeed = serializedObject.FindProperty("m_randomizePlaybackSpeed");
            m_modMinMaxPlaybackSpeed = serializedObject.FindProperty("m_modMinMaxPlaybackSpeed");
            m_minMaxPlaybackSpeed = serializedObject.FindProperty("m_minMaxPlaybackSpeed");

            ValuesOrEvents reqVal = (ValuesOrEvents)m_requirements.enumValueIndex;

            valuesGroup = new AnimBool((reqVal & ValuesOrEvents.Values) != 0, Repaint);
            eventsGroup = new AnimBool((reqVal & ValuesOrEvents.Events) != 0, Repaint);
            clipsVisible = new AnimBool(m_modClips.boolValue, Repaint);


            m_eventsReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_events, true, false, true, true);
            m_eventsReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_eventsReorderable.drawElementCallback = DrawEventElement;
            m_eventsReorderable.drawHeaderCallback = DrawEventHeader;

            m_valuesReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_values, true, false, true, true);
            m_valuesReorderable.drawElementCallback = DrawValuesElement;
            m_valuesReorderable.drawHeaderCallback = DrawValueHeader;
            m_valuesReorderable.elementHeight = 2f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            m_clipsReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_clipData, true, false, true, true);
            m_clipsReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_clipsReorderable.drawElementCallback = DrawClipElement;
            m_clipsReorderable.drawHeaderCallback = DrawClipHeader;

            if (m_editorUtils == null) {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }
        /// <summary> Main GUI function </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();

            m_editorUtils.Initialize(); // Do not remove this!
            m_editorUtils.GUIHeader();
            
            m_editorUtils.TitleNonLocalized(m_editorUtils.GetContent("ModifierHeader").text + serializedObject.targetObject.name);

            m_editorUtils.Panel("RequirementsPanel", RequirementsPanel, true);
            m_editorUtils.Panel("ModificationsPanel", ModificationsPanel, true);
            
            serializedObject.ApplyModifiedProperties();
        }
        /// <summary> Panel to display "Requirements" for Modifier to apply </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void RequirementsPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_editorUtils.PropertyField("mRequirements",m_requirements, inlineHelp);
            ValuesOrEvents reqVal = (ValuesOrEvents)System.Enum.GetValues(typeof(ValuesOrEvents)).GetValue(m_requirements.enumValueIndex);
            valuesGroup.target = (reqVal & ValuesOrEvents.Values) != 0;
            eventsGroup.target = (reqVal & ValuesOrEvents.Events) != 0;
            if (EditorGUILayout.BeginFadeGroup(eventsGroup.faded)) {
                m_editorUtils.InlineHelp("mEventsMix", inlineHelp);
                m_eventsReorderable.DoLayoutList();
                m_editorUtils.InlineHelp("mEvents", inlineHelp);
            }
            EditorGUILayout.EndFadeGroup();
            if (EditorGUILayout.BeginFadeGroup(valuesGroup.faded)) {
                m_editorUtils.InlineHelp("mValueMix", inlineHelp);
                m_valuesReorderable.DoLayoutList();
                m_editorUtils.InlineHelp("mValues", inlineHelp);
            }
            EditorGUILayout.EndFadeGroup();
            --EditorGUI.indentLevel;
        }

        /// <summary> Panel to display modifications to apply when active </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void ModificationsPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            ToggleField(EditorGUILayout.GetControlRect(), m_modClips, m_modClipsType, m_editorUtils.GetContent("mModClipsType"));
            m_editorUtils.InlineHelp("mModClipsType", inlineHelp);
            clipsVisible.target = m_modClips.boolValue;
            if (EditorGUILayout.BeginFadeGroup(clipsVisible.faded)) {
                if (m_clipsExpanded) {
                    m_clipsReorderable.DoLayoutList();
                } else {
                    int oldIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 1;
                    m_clipsExpanded = EditorGUILayout.Foldout(m_clipsExpanded, PropertyCount("mClips", m_clipData), true);
                    EditorGUI.indentLevel = oldIndent;
                }
                m_editorUtils.InlineHelp("mClips", inlineHelp);
                int badClipCount = 0;
                for (int x = 0; x < m_clipData.arraySize; ++x) {
                    AudioClip clip = m_clipData.GetArrayElementAtIndex(x).FindPropertyRelative("m_clip").objectReferenceValue as AudioClip;
                    if (clip != null) {
                        if (clip.loadType != AudioClipLoadType.DecompressOnLoad) {
                            ++badClipCount;
                        }
                    }
                }
                if (badClipCount > 0) {
                    EditorGUILayout.HelpBox(m_editorUtils.GetContent("InvalidClipMessage").text, MessageType.Warning, true);
                    if (m_editorUtils.ButtonRight("InvalidClipButton")) {
                        GUIContent progressBarContent = m_editorUtils.GetContent("InvalidClipPopup");
                        for (int x = 0; x < m_clipData.arraySize; ++x) {
                            AudioClip clip = m_clipData.GetArrayElementAtIndex(x).FindPropertyRelative("m_clip").objectReferenceValue as AudioClip;
                            EditorUtility.DisplayProgressBar(progressBarContent.text, progressBarContent.tooltip + clip.name, x / (float)badClipCount);
                            if (clip != null) {
                                if (clip.loadType != AudioClipLoadType.DecompressOnLoad) {
                                    AudioImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
                                    AudioImporterSampleSettings sampleSettings = importer.defaultSampleSettings;
                                    sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                                    importer.defaultSampleSettings = sampleSettings;
                                    if (importer.ContainsSampleSettingsOverride("Standalone")) {
                                        sampleSettings = importer.GetOverrideSampleSettings("Standalone");
                                        sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                                        importer.SetOverrideSampleSettings("Standalone", sampleSettings);
                                    }
                                    importer.SaveAndReimport();
                                }
                            }
                        }
                        EditorUtility.ClearProgressBar();
                    }
                } 
            }
            EditorGUILayout.EndFadeGroup();
            ToggleField(EditorGUILayout.GetControlRect(), m_modVolume, m_volume, m_editorUtils.GetContent("mVolume"));
            m_editorUtils.InlineHelp("mVolume", inlineHelp);
            ToggleField(EditorGUILayout.GetControlRect(), m_modPlaybackSpeed, m_playbackSpeed, m_editorUtils.GetContent("mPlaybackSpeed"));
            m_editorUtils.InlineHelp("mPlaybackSpeed", inlineHelp);
            ToggleField(EditorGUILayout.GetControlRect(), m_modRandomizePlaybackSpeed, m_randomizePlaybackSpeed, m_editorUtils.GetContent("mRandomizePlaybackSpeed"));
            m_editorUtils.InlineHelp("mRandomizePlaybackSpeed", inlineHelp);
            ToggleSliderRangeField(EditorGUILayout.GetControlRect(), m_modMinMaxPlaybackSpeed, m_minMaxPlaybackSpeed, m_editorUtils.GetContent("mMinMaxPlaybackSpeed"), 0.01f, 3f);
            m_editorUtils.InlineHelp("mMinMaxPlaybackSpeed", inlineHelp);
            ToggleField(EditorGUILayout.GetControlRect(), m_modDelayChance, m_delayChance, m_editorUtils.GetContent("mDelayChance"));
            m_editorUtils.InlineHelp("mDelayChance", inlineHelp);
            ToggleVector2Field(EditorGUILayout.GetControlRect(), m_modDelay, m_minMaxDelay, m_editorUtils.GetContent("mMinMaxDelay"), m_editorUtils.GetContent("mMinPrefix"), m_editorUtils.GetContent("mMaxPrefix"));
            m_editorUtils.InlineHelp("mMinMaxDelay", inlineHelp);
            ToggleField(EditorGUILayout.GetControlRect(), m_modRandomizeVolume, m_randomizeVolume, m_editorUtils.GetContent("mRandomizeVolume"));
            m_editorUtils.InlineHelp("mRandomizeVolume", inlineHelp);
            ToggleSliderRangeField(EditorGUILayout.GetControlRect(), m_modMinMaxVolume, m_minMaxVolume, m_editorUtils.GetContent("mMinMaxVolume"), 0.01f, 2f);
            m_editorUtils.InlineHelp("mMinMaxVolume", inlineHelp);
            --EditorGUI.indentLevel;
        }
        
        private GUIContent PropertyCount(string key, SerializedProperty property) {
            GUIContent content = m_editorUtils.GetContent(key);
            content.text += " [" + property.arraySize + "]";
            return content;
        }

        void DrawEventElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, m_events.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawEventHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, m_eventsMix, m_editorUtils.GetContent("mEventsMix"));
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawValuesElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, m_values.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawValueHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, m_valuesMix, m_editorUtils.GetContent("mValueMix"));
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawClipElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, m_clipData.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawClipHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            rect.xMin += 8f;
            m_clipsExpanded = EditorGUI.Foldout(rect, m_clipsExpanded, PropertyCount("mClips", m_clipData), true);
            EditorGUI.indentLevel = oldIndent;
        }
    }
}