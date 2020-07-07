// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using PWCommon2;
using AmbientSounds.Internal;

/*
 * Custom Editor for Sequence assets
 */

namespace AmbientSounds {
    [CustomPropertyDrawer(typeof(Sequence.ClipData))]
    public class SequenceClipDataEditor : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            position.width -= 120f;
            SerializedProperty clip = property.FindPropertyRelative("m_clip");
            SerializedProperty volume = property.FindPropertyRelative("m_volume");
            EditorGUI.ObjectField(position, clip, GUIContent.none);
            position.xMin = position.xMax;
            position.xMax += 120f;
            EditorGUI.Slider(position, volume, 0f, 1f, GUIContent.none);
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Sequence))]
    public class SequenceDataEditor : PWEditor, IPWEditor {
        /// <summary> Reference to EditorUtils for GUI functions </summary>
        private EditorUtils m_editorUtils;

        //Properties of Sequence asset
        SerializedProperty m_requirements;
        SerializedProperty m_clipData;
        SerializedProperty m_volume;
        SerializedProperty m_playbackSpeed;
        SerializedProperty m_randomizeOrder;
        SerializedProperty m_crossFade;
        SerializedProperty m_trackFadeTime;
        SerializedProperty m_volumeFadeTime;
        SerializedProperty m_playbackSpeedFadeTime;
        SerializedProperty m_delayChance;
        SerializedProperty m_delayFadeTime;
        SerializedProperty m_minMaxDelay;
        SerializedProperty m_valuesMix;
        SerializedProperty m_values;
        SerializedProperty m_eventsMix;
        SerializedProperty m_events;
        SerializedProperty m_randomVolume;
        SerializedProperty m_minMaxVolume;
        SerializedProperty m_randomPlaybackSpeed;
        SerializedProperty m_minMaxPlaybackSpeed;
        SerializedProperty m_modifiers;
        SerializedProperty m_outputType;
        SerializedProperty m_outputPrefab;
        SerializedProperty m_outputDirect;
        SerializedProperty m_outputDistance;
        SerializedProperty m_outputVerticalAngle;
        SerializedProperty m_outputHorizontalAngle;
        SerializedProperty m_syncGroup;
        SerializedProperty m_syncType;
        SerializedProperty m_eventsWhilePlaying;
        SerializedProperty m_valuesWhilePlaying;
        SerializedProperty m_outputFollowPosition = null;
        SerializedProperty m_outputFollowRotation = null;
        SerializedProperty m_OnPlayClip = null;
        SerializedProperty m_OnStopClip = null;

        /// <summary> Animated Boolean for if Output Distance should be shown </summary>
        AnimBool outputGroup;
        /// <summary> Animated Boolean for if Sliders should be displayed </summary>
        AnimBool slidersGroup;
        /// <summary> Animated Boolean for if Events should be displayed </summary>
        AnimBool eventsGroup;
        /// <summary> Animated Boolean for if OutputDirect is true </summary>
        AnimBool directGroup;
        /// <summary> Animated Boolean for if we are in a SyncGroup </summary>
        AnimBool syncGroup;
        UnityEditorInternal.ReorderableList m_eventsReorderable;
        UnityEditorInternal.ReorderableList m_valuesReorderable;
        bool m_clipsExpanded = true;
        UnityEditorInternal.ReorderableList m_clipsReorderable;
        bool m_modifiersExpanded = true;
        UnityEditorInternal.ReorderableList m_modifiersReorderable;
        UnityEditorInternal.ReorderableList m_eventsWhilePlayingReorderable;
        UnityEditorInternal.ReorderableList m_valuesWhilePlayingReorderable;

        /// <summary> Destructor to release references </summary>
        private void OnDestroy() {
            if (m_editorUtils != null) {
                m_editorUtils.Dispose();
            }
        }
        /// <summary> Constructor to set up references for editor </summary>
        void OnEnable() {
            m_requirements = serializedObject.FindProperty("m_requirements");
            m_clipData = serializedObject.FindProperty("m_clipData");
            m_volume = serializedObject.FindProperty("m_volume");
            m_playbackSpeed = serializedObject.FindProperty("m_playbackSpeed");
            m_randomizeOrder = serializedObject.FindProperty("m_randomizeOrder");
            m_crossFade = serializedObject.FindProperty("m_crossFade");
            m_trackFadeTime = serializedObject.FindProperty("m_trackFadeTime");
            m_volumeFadeTime = serializedObject.FindProperty("m_volumeFadeTime");
            m_playbackSpeedFadeTime = serializedObject.FindProperty("m_playbackSpeedFadeTime");
            m_delayFadeTime = serializedObject.FindProperty("m_delayFadeTime");
            m_delayChance = serializedObject.FindProperty("m_delayChance");
            m_minMaxDelay = serializedObject.FindProperty("m_minMaxDelay");
            m_valuesMix = serializedObject.FindProperty("m_valuesMix");
            m_values = serializedObject.FindProperty("m_values");
            m_eventsMix = serializedObject.FindProperty("m_eventsMix");
            m_events = serializedObject.FindProperty("m_events");
            m_randomVolume = serializedObject.FindProperty("m_randomVolume");
            m_minMaxVolume = serializedObject.FindProperty("m_minMaxVolume");
            m_randomPlaybackSpeed = serializedObject.FindProperty("m_randomPlaybackSpeed");
            m_minMaxPlaybackSpeed = serializedObject.FindProperty("m_minMaxPlaybackSpeed");
            m_modifiers = serializedObject.FindProperty("m_modifiers");
            m_outputType = serializedObject.FindProperty("m_outputType");
            m_outputPrefab = serializedObject.FindProperty("m_outputPrefab");
            m_outputDirect = serializedObject.FindProperty("m_outputDirect");
            m_outputDistance = serializedObject.FindProperty("m_outputDistance");
            m_outputVerticalAngle = serializedObject.FindProperty("m_outputVerticalAngle");
            m_outputHorizontalAngle = serializedObject.FindProperty("m_outputHorizontalAngle");
            m_syncGroup = serializedObject.FindProperty("m_syncGroup");
            m_syncType = serializedObject.FindProperty("m_syncType");
            m_eventsWhilePlaying = serializedObject.FindProperty("m_eventsWhilePlaying");
            m_valuesWhilePlaying = serializedObject.FindProperty("m_valuesWhilePlaying");
            m_outputFollowPosition = serializedObject.FindProperty("m_outputFollowPosition");
            m_outputFollowRotation = serializedObject.FindProperty("m_outputFollowRotation");
            m_OnPlayClip = serializedObject.FindProperty("m_OnPlayClip");
            m_OnStopClip = serializedObject.FindProperty("m_OnStopClip");

            OutputType outputType = (OutputType)System.Enum.GetValues(typeof(OutputType)).GetValue(m_outputType.enumValueIndex);
            ValuesOrEvents reqVal = (ValuesOrEvents)m_requirements.enumValueIndex;

            outputGroup = new AnimBool(m_outputDirect.boolValue || outputType != OutputType.STRAIGHT, Repaint);
            slidersGroup = new AnimBool((reqVal & ValuesOrEvents.Values) != 0, Repaint);
            eventsGroup = new AnimBool((reqVal & ValuesOrEvents.Events) != 0, Repaint);
            directGroup = new AnimBool(!m_outputDirect.boolValue, Repaint);
            syncGroup = new AnimBool(!string.IsNullOrEmpty(m_syncGroup.stringValue), Repaint);

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
            m_clipsReorderable.onAddCallback = OnAddClip;

            m_modifiersReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_modifiers, true, false, true, true);
            m_modifiersReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_modifiersReorderable.drawElementCallback = DrawModifierElement;
            m_modifiersReorderable.drawHeaderCallback = DrawModifierHeader;

            m_eventsWhilePlayingReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_eventsWhilePlaying, true, false, true, true);
            m_eventsWhilePlayingReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_eventsWhilePlayingReorderable.drawElementCallback = DrawEventsWhilePlayingElement;
            m_eventsWhilePlayingReorderable.drawHeaderCallback = DrawEventsWhilePlayingHeader;

            m_valuesWhilePlayingReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_valuesWhilePlaying, true, false, true, true);
            m_valuesWhilePlayingReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_valuesWhilePlayingReorderable.drawElementCallback = DrawValuesWhilePlayingElement;
            m_valuesWhilePlayingReorderable.drawHeaderCallback = DrawValuesWhilePlayingHeader;

            if (m_editorUtils == null) {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
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
            EditorGUI.PropertyField(rect, m_valuesMix, m_editorUtils.GetContent("mValuesMix"));
            EditorGUI.indentLevel = oldIndent;
        }
        void OnAddClip(UnityEditorInternal.ReorderableList list) {
            int idx = m_clipData.arraySize;
            m_clipData.InsertArrayElementAtIndex(idx);
            if(idx == 0)
                m_clipData.GetArrayElementAtIndex(idx).FindPropertyRelative("m_volume").floatValue = 1f;
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
        void DrawModifierElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.ObjectField(rect, m_modifiers.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawModifierHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            rect.xMin += 8f;
            m_modifiersExpanded = EditorGUI.Foldout(rect, m_modifiersExpanded, PropertyCount("mModifiers", m_modifiers), true);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawEventsWhilePlayingElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, m_eventsWhilePlaying.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawEventsWhilePlayingHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rect, m_editorUtils.GetContent("mEventsWhilePlaying"));
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawValuesWhilePlayingElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, m_valuesWhilePlaying.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawValuesWhilePlayingHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rect, m_editorUtils.GetContent("mValuesWhilePlaying"));
            EditorGUI.indentLevel = oldIndent;
        }
        /// <summary> Main GUI Function </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();

            m_editorUtils.Initialize(); // Do not remove this!
            m_editorUtils.GUIHeader();
            m_editorUtils.TitleNonLocalized(m_editorUtils.GetContent("SequenceDataHeader").text + serializedObject.targetObject.name);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (!Application.isPlaying || !AmbienceManager.WasSequenceAdded(target as Sequence)) {
                if (m_editorUtils.Button("EditorPlayButton")) {
                    Sequence seq = target as Sequence;
                    seq.m_forcePlay = true;
                    AmbienceManager.AddSequence(seq);
                }
            } else {
                if (m_editorUtils.Button("EditorStopButton")) {
                    Sequence seq = target as Sequence;
                    seq.m_forcePlay = false;
                    AmbienceManager.RemoveSequence(seq);
                }
            }
            EditorGUI.EndDisabledGroup();

            m_editorUtils.Panel("RequirementsPanel", RequirementsPanel, true);
            m_editorUtils.Panel("ClipsPanel", ClipsPanel, true);
            m_editorUtils.Panel("OutputPanel", OutputPanel, true);
            m_editorUtils.Panel("RandomizationPanel", RandomizationPanel, true);
            m_editorUtils.Panel("ModifiersPanel", ModifiersPanel, true);
            m_editorUtils.Panel("EventsPanel", EventsPanel, true);
            m_editorUtils.Panel("SyncPanel", SyncPanel, true);
            
            //m_editorUtils.GUIFooter();
            serializedObject.ApplyModifiedProperties();
        }
        /// <summary> Panel to show "Requirements" for Sequence to play </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        public void RequirementsPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            EditorGUI.BeginChangeCheck();
            m_editorUtils.PropertyField("mRequirements", m_requirements, inlineHelp);
            ValuesOrEvents reqVal = (ValuesOrEvents)System.Enum.GetValues(typeof(ValuesOrEvents)).GetValue(m_requirements.enumValueIndex);
            slidersGroup.target = (reqVal & ValuesOrEvents.Values) != 0;
            eventsGroup.target = (reqVal & ValuesOrEvents.Events) != 0;
            if (EditorGUILayout.BeginFadeGroup(eventsGroup.faded)) {
                m_editorUtils.InlineHelp("mEventsMix", inlineHelp);
                m_eventsReorderable.DoLayoutList();
                m_editorUtils.InlineHelp("mEvents", inlineHelp);
            }
            EditorGUILayout.EndFadeGroup();
            if (EditorGUI.EndChangeCheck() && MainWindowEditor.Instance != null)
                MainWindowEditor.Instance.Repaint();
            if (EditorGUILayout.BeginFadeGroup(slidersGroup.faded)) {
                m_editorUtils.InlineHelp("mValuesMix", inlineHelp);
                m_valuesReorderable.DoLayoutList();
                m_editorUtils.InlineHelp("mValues", inlineHelp);
            }
            EditorGUILayout.EndFadeGroup();
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to show "Clip" options of Sequence </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        public void ClipsPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            EditorGUI.BeginChangeCheck();
            Rect clipsRect;
            if (m_clipsExpanded) {
                clipsRect = EditorGUILayout.GetControlRect(true, m_clipsReorderable.GetHeight());
                m_clipsReorderable.DoList(clipsRect);
            } else {
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 1;
                m_clipsExpanded = EditorGUILayout.Foldout(m_clipsExpanded, PropertyCount("mClips", m_clipData), true);
                clipsRect = GUILayoutUtility.GetLastRect();
                EditorGUI.indentLevel = oldIndent;
            }
            if (Event.current.type == EventType.DragUpdated && clipsRect.Contains(Event.current.mousePosition)) {
                bool isValid = false;
                for (int i = 0; i < DragAndDrop.objectReferences.Length; ++i)
                    if (DragAndDrop.objectReferences[i] is AudioClip) {
                        isValid = true;
                        break;
                    }
                if (isValid)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            } else if (Event.current.type == EventType.DragPerform && clipsRect.Contains(Event.current.mousePosition)) {
                for (int i = 0; i < DragAndDrop.objectReferences.Length; ++i) {
                    if (!(DragAndDrop.objectReferences[i] is AudioClip))
                        continue;
                    int idx = m_clipData.arraySize;
                    m_clipData.InsertArrayElementAtIndex(idx);
                    SerializedProperty clipData = m_clipData.GetArrayElementAtIndex(idx);
                    clipData.FindPropertyRelative("m_volume").floatValue = 1f;
                    clipData.FindPropertyRelative("m_clip").objectReferenceValue = DragAndDrop.objectReferences[i];
                }
                Event.current.Use();
            }

            m_editorUtils.InlineHelp("mClips", inlineHelp);
            //PropertyCountField("mClips", true, m_clips, ((Sequence)target).m_clips.Length, inlineHelp);
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
                if(m_editorUtils.ButtonRight("InvalidClipButton")) {
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
            if (EditorGUI.EndChangeCheck() && Application.isPlaying) {
                (target as Sequence).UpdateModifiers();
            }
            m_editorUtils.PropertyField("mTrackFadeTime", m_trackFadeTime, inlineHelp);
            if (m_trackFadeTime.floatValue < 0f)
                m_trackFadeTime.floatValue = 0f;
            m_editorUtils.PropertyField("mVolume", m_volume, inlineHelp);
            m_editorUtils.PropertyField("mVolumeFadeTime", m_volumeFadeTime, inlineHelp);
            if (m_volumeFadeTime.floatValue < 0f)
                m_volumeFadeTime.floatValue = 0f;
            EditorGUI.BeginDisabledGroup(syncGroup.target && ((SyncType)System.Enum.GetValues(typeof(SyncType)).GetValue(m_syncType.enumValueIndex) & SyncType.FIT) > 0); {
                m_editorUtils.PropertyField("mPlaybackSpeed", m_playbackSpeed, inlineHelp);
                if (m_playbackSpeed.floatValue < 0f)
                    m_playbackSpeed.floatValue = 0f;
            } EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(syncGroup.target); {
                m_editorUtils.PropertyField("mPlaybackSpeedFadeTime", m_playbackSpeedFadeTime, inlineHelp);
                if (m_playbackSpeedFadeTime.floatValue < 0f)
                    m_playbackSpeedFadeTime.floatValue = 0f;
                m_editorUtils.PropertyField("mCrossFade", m_crossFade, inlineHelp);
                if (m_crossFade.floatValue < 0f)
                    m_crossFade.floatValue = 0f;
            } EditorGUI.EndDisabledGroup();
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to display Output options </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void OutputPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_editorUtils.EnumPopupLocalized("mOutputType", "OutputType_", m_outputType, inlineHelp);
            m_editorUtils.PropertyField("mOutputDirect", m_outputDirect, inlineHelp);
            OutputType outputType = (OutputType)System.Enum.GetValues(typeof(OutputType)).GetValue(m_outputType.enumValueIndex);
            outputGroup.target = m_outputDirect.boolValue || outputType != OutputType.STRAIGHT;
            directGroup.target = !m_outputDirect.boolValue;
            if (EditorGUILayout.BeginFadeGroup(outputGroup.faded)) {
                m_editorUtils.PropertyField("mOutputPrefab", m_outputPrefab, inlineHelp);
                if (m_outputPrefab!=null && m_outputPrefab.objectReferenceValue != null)
                {
                    AudioSource prefabSource = ((GameObject)m_outputPrefab.objectReferenceValue).GetComponent<AudioSource>();
                    if (prefabSource == null)
                    {
                        EditorGUILayout.HelpBox(m_editorUtils.GetContent("PrefabNoSource").text, MessageType.Info);
                    }
                    else
                    {
                        //bool spatialize = false;
                        bool spatialblend = false;
                        if (prefabSource.spatialBlend < 1f)
                            spatialblend = true;
                        if (/*spatialize ||//*/ spatialblend)
                        {
                            EditorGUILayout.HelpBox(m_editorUtils.GetContent("PrefabSettingsWarningPrefix").text
                             // + (spatialize ? m_editorUtils.GetContent("PrefabSettingsWarningSpatialize").text : "")
                              + (spatialblend ? m_editorUtils.GetContent("PrefabSettingsWarningSpatialBlend").text : "")
                              , MessageType.Warning);
                            if (m_editorUtils.Button("PrefabSettingsButton"))
                            {
                                //prefabSource.spatialize = true;
                                prefabSource.spatialBlend = 1.0f;
                                EditorUtility.SetDirty(m_outputPrefab.objectReferenceValue);
#if UNITY_2018_3_OR_NEWER
                            PrefabUtility.SavePrefabAsset(prefabSource.gameObject);
#else
                                PrefabUtility.ReplacePrefab(prefabSource.gameObject, m_outputPrefab.objectReferenceValue);
#endif
                            }
                        }
                    }
                }
                Rect r = EditorGUILayout.GetControlRect();
                float[] vals = new float[2] { m_outputDistance.vector2Value.x, m_outputDistance.vector2Value.y };
                EditorGUI.BeginChangeCheck();
                EditorGUI.MultiFloatField(r, m_editorUtils.GetContent("mOutputDistance"), new GUIContent[] { new GUIContent(""), new GUIContent("-") }, vals);
                if (EditorGUI.EndChangeCheck())
                    m_outputDistance.vector2Value = new Vector2(vals[0], vals[1]);
                m_editorUtils.InlineHelp("mOutputDistance", inlineHelp);
                m_editorUtils.SliderRange("mOutputVerticalAngle", m_outputVerticalAngle, inlineHelp, -180, 180);
                m_editorUtils.SliderRange("mOutputHorizontalAngle", m_outputHorizontalAngle, inlineHelp, -180, 180);
                m_outputFollowPosition.boolValue = m_editorUtils.Toggle("mOutputFollowPosition", m_outputFollowPosition.boolValue, inlineHelp);
                EditorGUI.BeginDisabledGroup(!m_outputFollowPosition.boolValue);
                m_outputFollowRotation.boolValue = m_editorUtils.Toggle("mOutputFollowRotation", m_outputFollowRotation.boolValue, inlineHelp);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFadeGroup();
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to show "Delay" options of Sequence </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        public void RandomizationPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_editorUtils.PropertyField("mRandomizeOrder", m_randomizeOrder, inlineHelp);

            EditorGUI.BeginDisabledGroup(syncGroup.target); {
                EditorGUILayout.Space();
                m_editorUtils.PropertyField("mDelayChance", m_delayChance, inlineHelp);
                m_editorUtils.PropertyField("mDelayFadeTime", m_delayFadeTime, inlineHelp);
                Rect r = EditorGUILayout.GetControlRect();
                float[] vals = new float[2] { m_minMaxDelay.vector2Value.x, m_minMaxDelay.vector2Value.y };
                EditorGUI.BeginChangeCheck();
                EditorGUI.MultiFloatField(r, m_editorUtils.GetContent("mMinMaxDelay"), new GUIContent[] { new GUIContent(""), new GUIContent("-") }, vals);
                if (EditorGUI.EndChangeCheck())
                    m_minMaxDelay.vector2Value = new Vector2(vals[0], vals[1]);
                m_editorUtils.InlineHelp("mMinMaxDelay", inlineHelp);
            } EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
            m_randomVolume.boolValue = m_editorUtils.Toggle("mRandomVolume", m_randomVolume.boolValue, inlineHelp);
            m_editorUtils.SliderRange("mMinMaxVolume", m_minMaxVolume, inlineHelp, 0f, 2f);
            m_randomPlaybackSpeed.boolValue = m_editorUtils.Toggle("mRandomPlaybackSpeed", m_randomPlaybackSpeed.boolValue, inlineHelp);
            m_editorUtils.SliderRange("mMinMaxPlaybackSpeed", m_minMaxPlaybackSpeed, inlineHelp, 0.01f, 3f);
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to show Modifiers list to apply to Sequence </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        public void ModifiersPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            EditorGUI.BeginChangeCheck();
            if (m_modifiersExpanded)
                m_modifiersReorderable.DoLayoutList();
            else {
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 1;
                m_modifiersExpanded = EditorGUILayout.Foldout(m_modifiersExpanded, PropertyCount("mModifiers", m_modifiers), true);
                EditorGUI.indentLevel = oldIndent;
            }
            m_editorUtils.InlineHelp("mModifiers", inlineHelp);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying) {
                (target as Sequence).UpdateModifiers();
            }
            --EditorGUI.indentLevel;
            
        }
        /// <summary> Panel to show Events list to apply when Sequence is playing </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        public void EventsPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_eventsWhilePlayingReorderable.DoLayoutList();
            m_editorUtils.InlineHelp("mEventsWhilePlaying", inlineHelp);
            m_valuesWhilePlayingReorderable.DoLayoutList();
            m_editorUtils.InlineHelp("mValuesWhilePlaying", inlineHelp);
            m_editorUtils.PropertyField("mOnPlayClip", m_OnPlayClip, inlineHelp);
            m_editorUtils.PropertyField("mOnStopClip", m_OnStopClip, inlineHelp);
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to show Sync Group </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        public void SyncPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_syncGroup.stringValue = m_editorUtils.TextField("mSyncGroup", m_syncGroup.stringValue, inlineHelp);
            syncGroup.target = !string.IsNullOrEmpty(m_syncGroup.stringValue);
            if (EditorGUILayout.BeginFadeGroup(syncGroup.faded)) {
                m_editorUtils.EnumPopupLocalized("mSyncType", "SyncType_", m_syncType, inlineHelp);
            }
            EditorGUILayout.EndFadeGroup();
            --EditorGUI.indentLevel;
        }
        
        private GUIContent PropertyCount(string key, SerializedProperty property) {
            GUIContent content = m_editorUtils.GetContent(key);
            content.text += " ["+property.arraySize+"]";
            return content;
        }
    }
}
