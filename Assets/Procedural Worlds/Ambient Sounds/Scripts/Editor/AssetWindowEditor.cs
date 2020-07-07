// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using UnityEditor;
using AmbientSounds.Internal;
using PWCommon2;
using System.Collections.Generic;

/*
 * Window to show all Assets for AmbientSounds system
 */

namespace AmbientSounds {
    /// <summary>
    /// Asset display window
    /// </summary>
    public class AssetWindowEditor : EditorWindow, IPWEditor {
        /// <summary> Is an AssetWindow open? </summary>
        public static bool IsOpen {
            get;
            private set;
        }
        /// <summary> Scroll position for GUI functions </summary>
        private Vector2 m_scrollPosition = Vector2.zero;
        /// <summary> Reference to EditorUtils to use for GUI functions </summary>
        private EditorUtils m_editorUtils;

        /// <summary> Did we start this DragAndDrop operation? </summary>
        bool weStartedDrag = false;
        /// <summary> Most common path for all Sequence assets </summary>
        string newSequencePath = "Assets/";
        /// <summary> Internal array of all Sequence assets found (use AllSequences property) </summary>
        static Sequence[] _allSequences = null;
        /// <summary> Array of all Sequence assets found in project </summary>
        Sequence[] AllSequences {
            get {
                if (_allSequences == null) {
                    Dictionary<string, int> pathCounts = new Dictionary<string, int>();
                    string[] sequenceGUIDs = AssetDatabase.FindAssets("t:AmbientSounds.Sequence");
                    List<Sequence> newSequences = new List<Sequence>(sequenceGUIDs.Length);
                    for (int x = 0; x < sequenceGUIDs.Length; ++x) {
                        string path = AssetDatabase.GUIDToAssetPath(sequenceGUIDs[x]);
                        AmbientSounds.Sequence data = AssetDatabase.LoadAssetAtPath<AmbientSounds.Sequence>(path);
                        if (data == null)
                            continue;
                        newSequences.Add(data);
                        path = path.Substring(0, path.LastIndexOf('/') + 1);
                        if (pathCounts.ContainsKey(path))
                            ++pathCounts[path];
                        else
                            pathCounts.Add(path, 1);
                    }
                    _allSequences = newSequences.ToArray();
                    int max = 0;
                    string bestPath = "Assets/";
                    foreach (KeyValuePair<string, int> path in pathCounts) {
                        if (path.Value > max) {
                            max = path.Value;
                            bestPath = path.Key;
                        }
                    }
                    newSequencePath = bestPath;
                }
                return _allSequences;
            }
        }
        /// <summary> Most common path for all Modifier assets </summary>
        string newModifierPath = "Assets/";
        /// <summary> Internal array of all Modifier assets found (use AllModifiers property) </summary>
        static Modifier[] _allModifiers = null;
        /// <summary> Array of all Modifier assets found in project </summary>
        Modifier[] AllModifiers {
            get {
                if (_allModifiers == null) {
                    Dictionary<string, int> pathCounts = new Dictionary<string, int>();
                    string[] sequenceGUIDs = AssetDatabase.FindAssets("t:AmbientSounds.Modifier");
                    List<Modifier> newModifiers = new List<Modifier>(sequenceGUIDs.Length);
                    for (int x = 0; x < sequenceGUIDs.Length; ++x) {
                        string path = AssetDatabase.GUIDToAssetPath(sequenceGUIDs[x]);
                        AmbientSounds.Modifier data = AssetDatabase.LoadAssetAtPath<AmbientSounds.Modifier>(path);
                        if (data == null)
                            continue;
                        newModifiers.Add(data);
                        path = path.Substring(0, path.LastIndexOf('/') + 1);
                        if (pathCounts.ContainsKey(path))
                            ++pathCounts[path];
                        else
                            pathCounts.Add(path, 1);
                    }
                    _allModifiers = newModifiers.ToArray();
                    int max = 0;
                    string bestPath = "Assets/";
                    foreach (KeyValuePair<string, int> path in pathCounts) {
                        if (path.Value > max) {
                            max = path.Value;
                            bestPath = path.Key;
                        }
                    }
                    newModifierPath = bestPath;
                }
                return _allModifiers;
            }
        }
        
        public bool PositionChecked { get; set; }
        Object lastClickedOn = null;

        #region Custom Menu Items
        /// <summary>
        /// Pulls up main window
        /// </summary>
        //[MenuItem("Window/" + PWConst.COMMON_MENU + "/Ambient Sounds/Assets...", false, 40)]
        public static void ShowWindow() {
            var win = GetWindow<AssetWindowEditor>(false, "Assets");
            win.Show();
        }
        #endregion

        #region Constructors destructors and related delegates
        /// <summary> Creates references for EditorUtils, Updates TabSet, and sets up project events </summary>
        private void OnEnable() {
            IsOpen = true;
            if (m_editorUtils == null) {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this);
            }
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
#else
            EditorApplication.playmodeStateChanged -= OnPlayModeChanged;
            EditorApplication.playmodeStateChanged += OnPlayModeChanged;
#endif
#if UNITY_2018
            EditorApplication.projectChanged -= OnAssetsChanged;
            EditorApplication.projectChanged += OnAssetsChanged;
#endif
            if (MainWindowEditor.Instance != null) {
                MainWindowEditor.Instance.Repaint();
            }
        }
        /// <summary>
        /// Clears _allSequences, _allModifiers, and _allValues so they can be rebuilt if needed again.
        /// </summary>
#if UNITY_2017_1_OR_NEWER
        [UnityEditor.Callbacks.DidReloadScripts]
#endif
        static void OnAssetsChanged() {
            _allSequences = null;
            _allModifiers = null;
        }
        /// <summary> Called when Application PlayMode changes (Fixes issue where AnimBool looses reference to Repaint when exiting play mode) </summary>
        /// <param name="pmsc">How PlayMode changed</param>
        void OnPlayModeChanged(
#if UNITY_2017_2_OR_NEWER
        PlayModeStateChange pmsc
#endif
            ) {
            m_editorUtils.Dispose();
            m_editorUtils = PWApp.GetEditorUtils(this);
            Repaint();
        }
        /// <summary> Destructor to release all created references and remove project events </summary>
        private void OnDestroy() {
            IsOpen = false;
#if UNITY_2018
            EditorApplication.projectChanged -= OnAssetsChanged;
#endif
            if (m_editorUtils != null) {
                m_editorUtils.Dispose();
            }

            if (MainWindowEditor.Instance != null) {
                MainWindowEditor.Instance.Repaint();
            }
        }
        #endregion

        #region GUI main
        /// <summary> Main GUI function </summary>
        void OnGUI() {
            IsOpen = true;
            m_editorUtils.Initialize(); // Do not remove this!
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);

            m_editorUtils.Panel("SequencesPanel", SequencesPanel, true);
            m_editorUtils.Panel("ModifiersPanel", ModifiersPanel, true);

            GUILayout.EndScrollView();
        }
        #endregion
        #region Tabs
        /// <summary> Displays a list of all Sequence assets in project </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void SequencesPanel(bool inlineHelp) {
            GUIStyle playButtonNormal;
            GUIStyle playButtonPressed;
            playButtonNormal = EditorStyles.miniButtonLeft;
            playButtonPressed = new GUIStyle(playButtonNormal);
            playButtonPressed.normal = playButtonNormal.active;
            GUIStyle muteButtonNormal;
            GUIStyle muteButtonPressed;
            muteButtonNormal = EditorStyles.miniButtonRight;
            muteButtonPressed = new GUIStyle(muteButtonNormal);
            muteButtonPressed.normal = muteButtonNormal.active;
            foreach (Sequence data in AllSequences) {
                EditorGUILayout.BeginHorizontal();
                Rect r = EditorGUILayout.GetControlRect();
                if (r.yMax > m_scrollPosition.y) {
                    if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) {
                        lastClickedOn = data;
                    } else if (Event.current.type == EventType.MouseDrag && lastClickedOn == data && r.Contains(Event.current.mousePosition)) {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { data };
                        DragAndDrop.StartDrag("Sequence:" + data.name);
                        Event.current.Use();
                        weStartedDrag = true;
                    } else if (Event.current.type == EventType.DragUpdated && r.Contains(Event.current.mousePosition)) {
                        bool found = false;
                        for (int o = 0; o < DragAndDrop.objectReferences.Length; ++o) {
                            if (DragAndDrop.objectReferences[o] is AudioClip) {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                                Event.current.Use();
                                found = true;
                                break;
                            }
                        }
                        if (!found) {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            Event.current.Use();
                        }
                    } else if (Event.current.type == EventType.DragPerform && r.Contains(Event.current.mousePosition)) {
                        List<Sequence.ClipData> clips = new List<Sequence.ClipData>(data.m_clipData);
                        bool foundOne = false;
                        for (int o = 0; o < DragAndDrop.objectReferences.Length; ++o) {
                            if (DragAndDrop.objectReferences[o] is AudioClip) {
                                foundOne = true;
                                clips.Add(DragAndDrop.objectReferences[o] as AudioClip);
                            }
                        }
                        if (foundOne)
                            data.m_clipData = clips.ToArray();
                    } else if ((Event.current.type == EventType.DragExited || Event.current.type == EventType.MouseUp) && lastClickedOn == data && weStartedDrag) {
                        weStartedDrag = false;
                        lastClickedOn = null;
                        DragAndDrop.PrepareStartDrag();
                    }
                }
                EditorGUI.ObjectField(r, data, typeof(Sequence), false);

                EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                if (!Application.isPlaying || !AmbienceManager.WasSequenceAdded(data)) {
                    if (m_editorUtils.Button(EditorGUIUtility.IconContent("PlayButton"), playButtonNormal, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(false))) {
                        data.m_forcePlay = true;
                        AmbienceManager.AddSequence(data);
                    }
                } else {
                    if (m_editorUtils.Button(EditorGUIUtility.IconContent("PlayButton On"), playButtonPressed, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(false))) {
                        data.m_forcePlay = false;
                        AmbienceManager.RemoveSequence(data);
                    }
                }

                Color startColor = GUI.color;
                GUIStyle buttonStyle = muteButtonNormal;
                if (data.m_forceMuted) {
                    GUI.color = Color.red;
                    buttonStyle = muteButtonPressed;
                }
                if (m_editorUtils.Button(EditorGUIUtility.IconContent("preAudioAutoPlayOff"), buttonStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(false))) {
                    if (Event.current.button == 1) {
                        data.m_forceMuted = false;
                        bool curMuted = true;
                        foreach (Sequence s in AllSequences)
                            if (s != null && s != data)
                                curMuted &= s.m_forceMuted;
                        foreach (Sequence s in AllSequences)
                            if (s != null && s != data)
                                s.m_forceMuted = !curMuted;
                    } else {
                        data.m_forceMuted = !data.m_forceMuted;
                    }
                }
                GUI.color = startColor;
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("SequenceCreateButton", GUILayout.ExpandWidth(false))) {
                GUIContent dialogContent = m_editorUtils.GetContent("SequenceCreateDialog");
                string newPath = EditorUtility.SaveFilePanelInProject(dialogContent.text, "Sequence", "asset", dialogContent.tooltip, newSequencePath);
                if (!string.IsNullOrEmpty(newPath)) {
                    AssetDatabase.CreateAsset(CreateInstance<Sequence>(), newPath);
                    Sequence newSequence = AssetDatabase.LoadAssetAtPath<Sequence>(newPath);
                    Selection.activeInstanceID = newSequence.GetInstanceID();
                }
            }
            Rect buttonRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.DragUpdated && buttonRect.Contains(Event.current.mousePosition)) {
                for (int o = 0; o < DragAndDrop.objectReferences.Length; ++o) {
                    if (DragAndDrop.objectReferences[o] is AudioClip) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                        break;
                    }
                }
            } else if (Event.current.type == EventType.DragPerform && buttonRect.Contains(Event.current.mousePosition)) {
                List<Sequence.ClipData> clips = new List<Sequence.ClipData>();
                for (int o = 0; o < DragAndDrop.objectReferences.Length; ++o)
                    if (DragAndDrop.objectReferences[o] is AudioClip)
                        clips.Add(DragAndDrop.objectReferences[o] as AudioClip);
                if (clips.Count > 0) {
                    if (Event.current.control) { //create one for each
                        GUIContent dialogContent = m_editorUtils.GetContent("SequenceMultiCreateDialog");
                        string saveFolder = EditorUtility.SaveFolderPanel(dialogContent.text, "Assets", "Assets");
                        if (!string.IsNullOrEmpty(saveFolder)) {
                            if (saveFolder.StartsWith(Application.dataPath)) {
                                saveFolder = saveFolder.Substring(Application.dataPath.Length - 6);
                                if (!saveFolder.EndsWith("/"))
                                    saveFolder += "/";
                                foreach (Sequence.ClipData clip in clips) {
                                    Sequence newSequence = CreateInstance<Sequence>();
                                    newSequence.m_clipData = new Sequence.ClipData[] { clip };
                                    AssetDatabase.CreateAsset(newSequence, saveFolder + clip.m_clip.name + ".asset");
                                }
                            } else {
                                GUIContent errorContent = m_editorUtils.GetContent("CreateWrongFolderError");
                                EditorUtility.DisplayDialog(errorContent.text, errorContent.tooltip, "Ok");
                            }
                        }
                    } else { //stack all into one sequence
                        GUIContent dialogContent = m_editorUtils.GetContent("SequenceCreateDialog");
                        string savePath = EditorUtility.SaveFilePanelInProject(dialogContent.text, "Sequence", "asset", dialogContent.tooltip);
                        if (!string.IsNullOrEmpty(savePath)) {
                            Sequence newSequence = CreateInstance<Sequence>();
                            newSequence.m_clipData = clips.ToArray();
                            AssetDatabase.CreateAsset(newSequence, savePath);
                        }
                    }
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("SequenceCreateButton", inlineHelp);
            --EditorGUI.indentLevel;
        }
        /// <summary> Displays a list of all Modifier assets in project </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void ModifiersPanel(bool inlineHelp) {
            foreach (Modifier mod in AllModifiers) {
                Rect r = EditorGUILayout.GetControlRect();
                if (r.yMax > m_scrollPosition.y) {
                    if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) {
                        lastClickedOn = mod;
                    } else if(Event.current.type == EventType.MouseDrag && lastClickedOn == mod && r.Contains(Event.current.mousePosition)) {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { mod };
                        DragAndDrop.StartDrag("Modifier:" + mod.name);
                        Event.current.Use();
                        weStartedDrag = true;
                    } else if ((Event.current.type == EventType.DragExited || Event.current.type == EventType.MouseUp) && lastClickedOn == mod && weStartedDrag) {
                        weStartedDrag = false;
                        lastClickedOn = null;
                        DragAndDrop.PrepareStartDrag();
                    }
                }
                EditorGUI.ObjectField(r, mod, typeof(Modifier), false);
            }
            ++EditorGUI.indentLevel;
            EditorGUILayout.BeginHorizontal();
            if (m_editorUtils.Button("ModifierCreateButton", GUILayout.ExpandWidth(false))) {
                GUIContent dialogContent = m_editorUtils.GetContent("ModifierCreateDialog");
                string newPath = EditorUtility.SaveFilePanelInProject(dialogContent.text, "Modifier", "asset", dialogContent.tooltip, newModifierPath);
                if (!string.IsNullOrEmpty(newPath)) {
                    AssetDatabase.CreateAsset(CreateInstance<Modifier>(), newPath);
                    Modifier newModifier = AssetDatabase.LoadAssetAtPath<Modifier>(newPath);
                    Selection.activeInstanceID = newModifier.GetInstanceID();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("ModifierCreateButton", inlineHelp);
            --EditorGUI.indentLevel;
        }
        #endregion
    }
}
