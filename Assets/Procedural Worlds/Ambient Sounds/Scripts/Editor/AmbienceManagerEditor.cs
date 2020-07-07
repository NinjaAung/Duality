// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using PWCommon2;
using AmbientSounds.Internal;
using UnityEditor.AnimatedValues;

/*
 * Custom Editor for AmbienceManager Components
 */

namespace AmbientSounds {
    [CustomEditor(typeof(AmbienceManager))]
    public class AmbienceManagerEditor : PWEditor, IPWEditor {
        /// <summary> Reference to EditorUtils for GUI functions </summary>
        private EditorUtils m_editorUtils;

        //Property references for AmbienceManager object
        SerializedProperty m_playerObject;
        SerializedProperty m_playSpeed;
        SerializedProperty m_volume;
        SerializedProperty m_globalSequences;
        SerializedProperty m_preloadAudio;
        SerializedProperty m_useAudioSource;
        SerializedProperty m_audioSourcePrefab;
        SerializedProperty m_audioSourceChannels;

        AnimBool audioSourceGroup = null;

        /// <summary> Current Event name being entered in debug menu </summary>
        string tmpTrigger = "";
        /// <summary> Current Value name being entered in debug menu </summary>
        string tmpValueName = "";
        /// <summary> Current Value value being entered in debug menu </summary>
        float tmpValueValue = 0f;
        /// <summary> List of all Sequence assets being played for debug menu </summary>
        List<Sequence> GlobalSequences = new List<Sequence>();
        /// <summary> List of all AudioArea objects in scene for debug menu </summary>
        List<AudioArea> AreaSequences = new List<AudioArea>();
        List<AmbienceManager.TrackPlayingInfo> AllTracks = new List<AmbienceManager.TrackPlayingInfo>();
        Dictionary<Sequence, string> GlobalBlocked = new Dictionary<Sequence, string>();
        /// <summary> Did we start this DragAndDrop operation? </summary>
        bool weStartedDrag = false;

        UnityEditorInternal.ReorderableList m_globalSequencesReorderable;

        /// <summary> Destructor to release references </summary>
        private void OnDestroy() {
            if (m_editorUtils != null) {
                m_editorUtils.Dispose();
            }
        }
        /// <summary> Constructor to set up references </summary>
        public void OnEnable() {
            m_playerObject = serializedObject.FindProperty("m_playerObject");
            m_playSpeed = serializedObject.FindProperty("m_playSpeed");
            m_volume = serializedObject.FindProperty("m_volume");
            m_globalSequences = serializedObject.FindProperty("m_globalSequences");
            m_preloadAudio = serializedObject.FindProperty("m_preloadAudio");
            m_useAudioSource = serializedObject.FindProperty("m_useAudioSource");
            m_audioSourcePrefab = serializedObject.FindProperty("m_audioSourcePrefab");
            m_audioSourceChannels = serializedObject.FindProperty("m_audioSourceChannels");

            audioSourceGroup = new AnimBool(m_useAudioSource.boolValue, Repaint);

            m_globalSequencesReorderable = new UnityEditorInternal.ReorderableList(serializedObject, m_globalSequences, true, true, true, true);
            m_globalSequencesReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_globalSequencesReorderable.drawElementCallback = DrawSequenceElement;
            m_globalSequencesReorderable.drawHeaderCallback = DrawSequenceHeader;
            m_globalSequencesReorderable.onRemoveCallback = OnRemovedGlobalSequence;

            if (m_editorUtils == null) {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
        }
        /// <summary> Main GUI function </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();

            m_editorUtils.Initialize(); // Do not remove this!

            m_editorUtils.Panel("GeneralPanel", GeneralPanel, true);
            m_editorUtils.Panel("GlobalSequencesPanel", GlobalSequencesPanel, true);
            m_editorUtils.Panel("OutputPanel", OutputPanel, true);

            if (EditorApplication.isPlaying) {
                AmbienceManager.GetSequences(ref GlobalSequences, ref AreaSequences);
                AllTracks = AmbienceManager.GetTracks();
                GlobalBlocked = AmbienceManager.GetBlocked();
                if (AreaSequences.Count > 0)
                    m_editorUtils.Panel("AreaAudioPanel", AreaAudioPanel, true);
                if (GlobalSequences.Count > 0)
                    m_editorUtils.Panel("AddedSequencesPanel", AddedSequencesPanel, true);
                if (AllTracks.Count > 0)
                    m_editorUtils.Panel("CurrentlyPlayingPanel", CurrentlyPlayingPanel, true);
                if (GlobalBlocked.Count > 0)
                    m_editorUtils.Panel("CurrentlyBlockedPanel", CurrentlyBlockedPanel, true);
                m_editorUtils.Panel("ValuesPanel", ValuesPanel, true);
                m_editorUtils.Panel("EventsPanel", EventsPanel, true);
                EditorUtility.SetDirty(target);
            }

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.DragUpdated && DragAndDrop.objectReferences.Length > 0) {
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Sequence && string.IsNullOrEmpty((DragAndDrop.objectReferences[x] as Sequence).m_syncGroup)) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        break;
                    }
                }
            } else if (currentEvent.type == EventType.DragPerform && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Sequence) {
                        Sequence seq = DragAndDrop.objectReferences[x] as Sequence;
                        m_globalSequences.InsertArrayElementAtIndex(m_globalSequences.arraySize);
                        m_globalSequences.GetArrayElementAtIndex(m_globalSequences.arraySize - 1).objectReferenceValue = seq;
                        foundOne = true;
                    }
                }
                if (foundOne) {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        /// <summary> Panel to show "General" options </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void GeneralPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_editorUtils.PropertyField("mPlayerObject", m_playerObject, inlineHelp);
            m_editorUtils.PropertyField("mPlaySpeed", m_playSpeed, inlineHelp);
            m_editorUtils.PropertyField("mVolume", false, m_volume, inlineHelp);
            
            EditorGUI.BeginChangeCheck();
            m_editorUtils.PropertyField("mPreloadAudio", m_preloadAudio, inlineHelp);
            if (EditorGUI.EndChangeCheck())
                AmbienceManager.s_preloadAudio = m_preloadAudio.boolValue;
            --EditorGUI.indentLevel;
        }
        void DrawSequenceElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.ObjectField(rect, m_globalSequences.GetArrayElementAtIndex(index), GUIContent.none);
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawSequenceHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rect, PropertyCount("GlobalSequencesPanel", m_globalSequences));
            EditorGUI.indentLevel = oldIndent;
        }
        void OnRemovedGlobalSequence(UnityEditorInternal.ReorderableList list) {
            if (list.index < 0 || list.index >= list.count)
                return;
            Sequence toRemove = m_globalSequences.GetArrayElementAtIndex(list.index).objectReferenceValue as Sequence;
            m_globalSequences.DeleteArrayElementAtIndex(list.index);
            if (Application.isPlaying && toRemove != null && !AmbienceManager.WasSequenceAdded(toRemove))
                AmbienceManager.OnEditorRemovedSequence(toRemove);
        }
        private GUIContent PropertyCount(string key, SerializedProperty property) {
            GUIContent content = m_editorUtils.GetContent(key);
            content.text += " [" + property.arraySize + "]";
            return content;
        }
        /// <summary> Panel to show "Global Sequences" </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void GlobalSequencesPanel(bool inlineHelp) {
            m_globalSequencesReorderable.DoLayoutList();
        }
        /// <summary> Panel to show "Output" options </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void OutputPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            m_useAudioSource.boolValue = m_editorUtils.Toggle("mUseAudioSource", m_useAudioSource.boolValue, inlineHelp);
            audioSourceGroup.target = m_useAudioSource.boolValue;
            if (EditorGUILayout.BeginFadeGroup(audioSourceGroup.faded)) {
                m_audioSourcePrefab.objectReferenceValue = m_editorUtils.ObjectField("mAudioSourcePrefab", m_audioSourcePrefab.objectReferenceValue, typeof(GameObject), false, inlineHelp);
                m_audioSourceChannels.intValue = Mathf.Clamp(m_editorUtils.IntField("mAudioSourceChannels", m_audioSourceChannels.intValue, inlineHelp), 1, 8);
            }
            EditorGUILayout.EndFadeGroup();
            --EditorGUI.indentLevel;
        }

        /// <summary> Panel to show all currently playing AudioClips </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void CurrentlyPlayingPanel(bool inlineHelp) {
            Event currentEvent = Event.current;
            GUIStyle muteButtonNormal;
            GUIStyle muteButtonPressed;
            muteButtonNormal = EditorStyles.miniButton;
            muteButtonPressed = new GUIStyle(muteButtonNormal);
            muteButtonPressed.normal = muteButtonNormal.active;
            foreach (AmbienceManager.TrackPlayingInfo track in AllTracks) {
                EditorGUILayout.BeginHorizontal();
                if (track.m_sequence != null) {
                    Color startColor = GUI.color;
                    GUIStyle buttonStyle = muteButtonNormal;
                    if (track.m_sequence.m_forceMuted) {
                        GUI.color = Color.red;
                        buttonStyle = muteButtonPressed;
                    }
                    if (m_editorUtils.Button(EditorGUIUtility.IconContent("preAudioAutoPlayOff"), buttonStyle, GUILayout.ExpandWidth(false))) {
                        if (Event.current.button == 1) {
                            track.m_sequence.m_forceMuted = false;
                            bool curMuted = true;
                            foreach (AmbienceManager.TrackPlayingInfo t in AllTracks)
                                if (t.m_sequence != null && t.m_sequence != track.m_sequence)
                                    curMuted &= t.m_sequence.m_forceMuted;
                            foreach (AmbienceManager.TrackPlayingInfo t in AllTracks)
                                if (t.m_sequence != null && t.m_sequence != track.m_sequence)
                                    t.m_sequence.m_forceMuted = !curMuted;

                        } else {
                            track.m_sequence.m_forceMuted = !track.m_sequence.m_forceMuted;
                        }
                    }
                    GUI.color = startColor;
                }
                Rect r = EditorGUILayout.GetControlRect();
                EditorGUILayout.EndHorizontal();
                Rect labelRect = r;
                labelRect.xMin += EditorGUIUtility.labelWidth;
                GUI.Label(labelRect, new GUIContent(track.m_name, track.m_name));
                labelRect.xMax = labelRect.xMin;
                labelRect.xMin = r.xMin;
                Rect meterRect = labelRect;
                meterRect.xMin = meterRect.xMax - meterRect.height;
                if (currentEvent.type == EventType.MouseDown && meterRect.Contains(currentEvent.mousePosition))
                    currentEvent.Use();
                else if (currentEvent.type == EventType.MouseDrag && r.Contains(currentEvent.mousePosition)) {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { track.m_sequence };
                    DragAndDrop.StartDrag("Sequence:" + track.m_sequence.name);
                    currentEvent.Use();
                    weStartedDrag = true;
                } else if ((currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.DragExited) && weStartedDrag) {
                    weStartedDrag = false;
                    DragAndDrop.PrepareStartDrag();
                }
                EditorGUI.ObjectField(labelRect, track.m_sequence, typeof(Sequence), false);
                meterRect.xMin = labelRect.xMax - r.height * 0.75f;
                meterRect.xMax -= labelRect.height * 0.125f;
                EditorGUI.DrawRect(meterRect, Color.black);
                Rect levelRect = new Rect(meterRect);
                levelRect.yMin = meterRect.yMax - (meterRect.yMax - meterRect.yMin) * track.m_volumeLevel;
                EditorGUI.DrawRect(levelRect, Color.green);
                levelRect.yMin = meterRect.yMax - (meterRect.yMax - meterRect.yMin) * track.m_fadeLevel;
                levelRect.yMax = levelRect.yMin + 2f;
                EditorGUI.DrawRect(levelRect, Color.red);
                
            }
        }
        /// <summary> Panel to show all Blocked Sequences </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void CurrentlyBlockedPanel(bool inlineHelp) {
            Event currentEvent = Event.current;
            foreach (KeyValuePair<Sequence, string> seq in GlobalBlocked) {
                EditorGUILayout.BeginHorizontal();
                Rect r = EditorGUILayout.GetControlRect();
                Rect labelRect = r;
                labelRect.xMin += EditorGUIUtility.labelWidth;
                GUI.Label(labelRect, new GUIContent(seq.Value, seq.Value));
                labelRect.xMax = labelRect.xMin;
                labelRect.xMin = r.xMin;
                Rect meterRect = labelRect;
                meterRect.xMin = meterRect.xMax - meterRect.height;
                if (currentEvent.type == EventType.MouseDown && meterRect.Contains(currentEvent.mousePosition))
                    currentEvent.Use();
                else if (currentEvent.type == EventType.MouseDrag && r.Contains(currentEvent.mousePosition)) {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { seq.Key };
                    DragAndDrop.StartDrag("Sequence:" + seq.Key.name);
                    currentEvent.Use();
                    weStartedDrag = true;
                } else if ((currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.DragExited) && weStartedDrag) {
                    weStartedDrag = false;
                    DragAndDrop.PrepareStartDrag();
                }
                EditorGUI.ObjectField(labelRect, seq.Key, typeof(Sequence), false);
                meterRect.xMin = labelRect.xMax - r.height * 0.75f;
                meterRect.xMax -= labelRect.height * 0.125f;
                if (m_editorUtils.Button("ForceUnblockSequence", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false))) {
                    AmbienceManager.Debug_UnBlockSequence(seq.Key);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        /// <summary> Panel to show all AudioAreas in scene while playing </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void AreaAudioPanel(bool inlineHelp) {
            Event currentEvent = Event.current;
            foreach (AudioArea ps in AreaSequences) {
                Rect r = EditorGUILayout.GetControlRect();
                Rect meterRect = r;
                meterRect.xMin = meterRect.xMax - meterRect.height;
                if (currentEvent.type == EventType.MouseDown && meterRect.Contains(currentEvent.mousePosition))
                    currentEvent.Use();
                else if (currentEvent.type == EventType.MouseDrag && r.Contains(currentEvent.mousePosition)) {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { ps };
                    DragAndDrop.StartDrag("AudioArea:" + ps.name);
                    currentEvent.Use();
                    weStartedDrag = true;
                } else if (currentEvent.type == EventType.MouseUp && weStartedDrag) {
                    weStartedDrag = false;
                    DragAndDrop.PrepareStartDrag();
                }
                EditorGUI.ObjectField(r, ps, typeof(Sequence), false);
                meterRect.xMin = r.xMax - r.height * 0.75f;
                meterRect.xMax -= r.height * 0.125f;
                EditorGUI.DrawRect(meterRect, Color.black);
                meterRect.yMin = meterRect.yMax - (meterRect.yMax - meterRect.yMin) * ps.m_lastFade;
                EditorGUI.DrawRect(meterRect, Color.green);
            }
        }
        /// <summary> Panel to show all "Global" Sequences that have been added via script while playing </summary>
        /// <param name="inlineHelp">Should help be displayed? </param>
        void AddedSequencesPanel(bool inlineHelp) {
            foreach (Sequence data in GlobalSequences) {
                EditorGUILayout.BeginHorizontal();
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.MouseDrag && r.Contains(Event.current.mousePosition)) {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new Object[] { data };
                    DragAndDrop.StartDrag("Sequence:" + data.name);
                    Event.current.Use();
                    weStartedDrag = true;
                } else if ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.DragExited) && weStartedDrag) {
                    weStartedDrag = false;
                    DragAndDrop.PrepareStartDrag();
                }
                EditorGUI.ObjectField(r, data, typeof(Sequence), false);
                if (m_editorUtils.Button("RemoveSequenceButton", GUILayout.ExpandWidth(false))) {
                    AmbienceManager.RemoveSequence(data);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        /// <summary> Panel to show all active "Values" and thier values while playing </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void ValuesPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            {
                if (m_editorUtils.Button("AddValueButton", GUILayout.Width(40)) && !string.IsNullOrEmpty(tmpValueName)) {
                    AmbienceManager.SetValue(tmpValueName, tmpValueValue);
                    tmpValueName = "";
                    tmpValueValue = 0f;
                }
                tmpValueName = GUILayout.TextField(tmpValueName, GUILayout.ExpandWidth(true)); //GUILayout.Width(Mathf.Clamp(EditorStyles.textField.CalcSize(new GUIContent(tmpValueName + "|")).x, 80f, EditorGUIUtility.currentViewWidth * 0.5f)));
                //tmpValueValue = EditorGUILayout.Slider(tmpValueValue, 0f, 1f);
            }
            EditorGUILayout.EndHorizontal();
            foreach (KeyValuePair<string, float> Value in AmbienceManager.GetValues()) {
                EditorGUILayout.BeginHorizontal();
                {
                    if (m_editorUtils.Button("RemoveValueButton", GUILayout.Width(40)))
                        AmbienceManager.RemoveValue(Value.Key);
                    string label = Value.Key;
                    if (label.Length > 25)
                        label = label.Substring(0, 25);
                    GUILayout.Label(new GUIContent(label, Value.Key), GUILayout.ExpandWidth(false));
                    EditorGUI.BeginChangeCheck();
                    float tmpVal = EditorGUILayout.Slider(Value.Value, 0f, 1f, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                        AmbienceManager.SetValue(Value.Key, tmpVal);
                }
                EditorGUILayout.EndHorizontal();
            }
            --EditorGUI.indentLevel;
        }
        /// <summary> Panel to show all active "Events" while playing </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void EventsPanel(bool inlineHelp) {
            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            {
                if (m_editorUtils.Button("AddEventButton", inlineHelp, GUILayout.Width(40)) && !string.IsNullOrEmpty(tmpTrigger)) {
                    AmbienceManager.ActivateEvent(tmpTrigger);
                    tmpTrigger = "";
                }
                tmpTrigger = GUILayout.TextField(tmpTrigger);
            }
            EditorGUILayout.EndHorizontal();
            foreach (string str in AmbienceManager.GetEvents()) {
                EditorGUILayout.BeginHorizontal();
                {
                    if (m_editorUtils.Button("RemoveEventButton", GUILayout.Width(40)))
                        AmbienceManager.DeactivateEvent(str);
                    GUILayout.Label(str);
                }
                EditorGUILayout.EndHorizontal();
            }
            --EditorGUI.indentLevel;
        }
    }
}
