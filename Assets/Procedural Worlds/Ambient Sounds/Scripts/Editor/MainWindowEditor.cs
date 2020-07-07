// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using AmbientSounds.Internal;
using PWCommon2;
using System.Collections.Generic;

/*
 * Main "Manager" Window to display all settings of system in one place
 */

namespace AmbientSounds
{
    /// <summary>
    /// Main Workflow Editor Window
    /// </summary>
    public class MainWindowEditor : EditorWindow, IPWEditor
    {
        /// <summary> Reference to MainWindowEditor for other scripts to access </summary>
        public static MainWindowEditor Instance {
            get;
            private set;
        }
        #region ValueInfo Class
        /// <summary> Contains information about which Modifiers and Sequences use a Value </summary>
        class AssetInfo {
            /// <summary> Name of the Value </summary>
            public string Name = "";
            /// <summary> List of all Sequences that reference this Value </summary>
            public List<Sequence> Sequences;
            /// <summary> List of all Modifiers that reference this Value </summary>
            public List<Modifier> Modifiers;

            /// <summary> Constructor with a Sequence that references passed Value </summary>
            /// <param name="name">Name of Value</param>
            /// <param name="sequence">Sequence that references this Value</param>
            public AssetInfo(string name, Sequence sequence = null) {
                Name = name;
                Sequences = new List<Sequence>();
                Modifiers = new List<Modifier>();
                if (sequence)
                    Sequences.Add(sequence);
            }
            /// <summary> Constructor with a Modifier that references passed Value </summary>
            /// <param name="name">Name of Value</param>
            /// <param name="modifier">Modifier that references this Value</param>
            public AssetInfo(string name, Modifier modifier) {
                Name = name;
                Sequences = new List<Sequence>();
                Modifiers = new List<Modifier>();
                if (modifier)
                    Modifiers.Add(modifier);
            }
        }
        /// <summary> Contains information about which Value a context menu item is referencing </summary>
        struct ValueContextInfo {
            /// <summary> Value to edit </summary>
            public SliderRange Value;
            /// <summary> Sequence or Modifier that contains this Value </summary>
            public Object parent;
            /// <summary> Constructor for ValueContextInfo </summary>
            /// <param name="s">Value to edit</param>
            /// <param name="obj">Sequence or Modifier that contains this Value</param>
            public ValueContextInfo(SliderRange s, Object obj) {
                Value = s;
                parent = obj;
            }
        }
        #endregion
        /// <summary> Shared scroll position for all tabs </summary>
        private Vector2 m_scrollPosition = Vector2.zero;
        /// <summary> Reference to EditorUtils to use for GUI functions </summary>
        private EditorUtils m_editorUtils;

        /// <summary> Reference to the AmbienceManager in this Scene </summary>
        AmbienceManager AmbienceManagerInstance = null;
        /// <summary> Collection of Tabs to display </summary>
        TabSet mainTabs = null;
        /// <summary> Did we start this DragAndDrop operation? </summary>
        bool weStartedDrag = false;
        /// <summary> Internal reference to EditorUtils CommonStyles class (use EditorSkin property) </summary>
        EditorUtils.CommonStyles _editorSkin = null;
        /// <summary> Reference to EditorUtils CommonStyles class for GUI functions </summary>
        EditorUtils.CommonStyles EditorSkin {
            get {
                if (_editorSkin == null) {
                    _editorSkin = new EditorUtils.CommonStyles();
                }
                return _editorSkin;
            }
        }

        /// <summary> Most common path for all Sequence assets </summary>
        string newSequencePath = "Assets/";
        /// <summary> Internal array of all Sequence objects found (use AllSequences property) </summary>
        static Sequence[] _allSequences = null;
        /// <summary> Array of all Sequence objects found in project </summary>
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
        /// <summary> Internal array of all Modifier objects found (use AllModifiers property) </summary>
        static Modifier[] _allModifiers = null;
        /// <summary> Array of all Modifier objects found in project </summary>
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

        //Value edit variables
        /// <summary> Reference to ValueRange that is currently being edited </summary>
        SliderRange curValueEditing = null;
        /// <summary> Starting mouse X Position of current edit operation </summary>
        float curValueEditingStartPos = 0f;
        /// <summary> Starting "min" value of current edit operation </summary>
        float curValueEditingStartMin = 0f;
        /// <summary> Starting "max" value of current edit operation </summary>
        float curValueEditingStartMax = 0f;
        /// <summary> Type of edit operation 0=None 1=Min&Max 2=Min 3=Max 4=MinFalloff 5=MaxFalloff </summary>
        int curValueEditingType = 0;
        /// <summary> Number of vertical dividers to draw on Value tab </summary>
        int ValueGuidelineCount = 3;
        /// <summary> Should Values snap to vertical lines when dragging in Values Tab? </summary>
        bool ValueGuidelineSnap = true;
        /// <summary> Is "inlineHelp" active for Value tab? </summary>
        bool valuesHelpActive = false;
        /// <summary> Is "inlineHelp" active for Event tab? </summary>
        bool eventHelpActive = false;
        /// <summary> Is "inlineHelp" active for Sync Groups tab? </summary>
        bool syncHelpActive = false;
        /// <summary> Was the "AssetsWindow" button pressed this event? (needed to open at the beginning of next event after press) </summary>
        bool wantsOpenAssets = false;

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

        Object lastClickedOn = null;
        static int curSpatializerPluginIdx = 0;
#if UNITY_2017_2_OR_NEWER
        static List<string> allSpatializers = new List<string>();
        static bool hasMicrosoftSpatializer = false, hasOculusSpatializer = false;
#endif
        const int FalloffTextureSize = 50;
        Texture2D _LeftFalloffTexture = null;
        Texture2D LeftFalloffTexture {
            get {
                if (true) { //(_LeftFalloffTexture == null) {
                    _LeftFalloffTexture = new Texture2D(FalloffTextureSize, FalloffTextureSize);
                    _LeftFalloffTexture.wrapMode = TextureWrapMode.Clamp;
                    for (int x = 0; x < FalloffTextureSize; ++x) {
                        for (int y = 0; y < FalloffTextureSize; ++y) {
                            _LeftFalloffTexture.SetPixel(x, y, new Color(1f, 1f, 1f, x + (FalloffTextureSize - y) > FalloffTextureSize ? 1f : 0f));
                        }
                    }
                    _LeftFalloffTexture.Apply();
                }
                return _LeftFalloffTexture;
            }
        }
        Texture2D _RightFalloffTexture = null;
        Texture2D RightFalloffTexture {
            get {
                if (true) { //(_LeftFalloffTexture == null) {
                    _RightFalloffTexture = new Texture2D(FalloffTextureSize, FalloffTextureSize);
                    _RightFalloffTexture.wrapMode = TextureWrapMode.Clamp;
                    for (int x = 0; x < FalloffTextureSize; ++x) {
                        for (int y = 0; y < FalloffTextureSize; ++y) {
                            _RightFalloffTexture.SetPixel(x, y, new Color(1f, 1f, 1f, (FalloffTextureSize - x) + (FalloffTextureSize - y) > FalloffTextureSize ? 1f : 0f));
                        }
                    }
                    _RightFalloffTexture.Apply();
                }
                return _RightFalloffTexture;
            }
        }

        Color[] SyncGroupTrackColors = new Color[] {
            new Color(0f, 0.5f, 0f, 1f),
            new Color(0.5f, 0f, 0f, 1f),
            new Color(0f, 0f, 0.5f, 1f),
            new Color(0.5f, 0.5f, 0f, 1f),
            new Color(0.5f, 0f, 0.5f, 1f),
            new Color(0f, 0.5f, 0.5f, 1f),
            new Color(0.25f, 0.75f, 0f, 1f),
            new Color(0.25f, 0f, 0.75f, 1f),
            new Color(0f, 0.25f, 0.75f, 1f),
            new Color(0.75f, 0.25f, 0f, 1f),
            new Color(0.75f, 0f, 0.25f, 1f),
            new Color(0f, 0.75f, 0.25f, 1f),
        };

        AnimBool Manager_UseAudioSourceGroup = null;
        
        /// <summary> Required bool property for IPWEditor </summary>
        public bool PositionChecked { get; set; }
        
        UnityEditorInternal.ReorderableList m_globalSequencesReorderable;

        #region Custom Menu Items
        /// <summary>
        /// Pulls up main window
        /// </summary>
        [MenuItem("Window/" + PWConst.COMMON_MENU + "/Ambient Sounds/Ambient Sounds Manager...", false, 40)]
        public static void ShowWindow()
        {
            if (Instance == null)
                Instance = GetWindow<MainWindowEditor>(false, "Ambient Sounds Manager");
            Instance.Show();
        }
        /// <summary>
        /// Entry to create an Ambience Manager from Scene Hierarchy create menu
        /// </summary>
        [MenuItem("GameObject/Procedural Worlds/AmbientSounds/Ambience Manager", false, 0)]
        static void CreateAmbienceManager(MenuCommand menuCommand)
        {
            // Create a new game object
            GameObject go = new GameObject("Ambience Manager");
            // Add the Ambience Manager
            go.AddComponent<AmbienceManager>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        /// <summary>
        /// Entry to create an Audio Area from Scene Hierarchy create menu
        /// </summary>
        [MenuItem("GameObject/Procedural Worlds/AmbientSounds/Audio Area", false, 10)]
        static void CreateAudioArea(MenuCommand menuCommand)
        {
            // Create a new game object
            GameObject go = new GameObject("Audio Area");
            // Add the Ambience Manager
            go.AddComponent<AudioArea>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
        #endregion

        #region Constructors destructors and related delegates
        /// <summary> Creates references for EditorUtils, Updates TabSet, and sets up project events </summary>
        private void OnEnable() {
            if (m_editorUtils == null) {
                // Get editor utils for this
                m_editorUtils = PWApp.GetEditorUtils(this, UpdateTabs);
                AmbienceManagerInstance = FindObjectOfType<AmbienceManager>();
            } else if (AmbienceManagerInstance == null) {
                AmbienceManagerInstance = FindObjectOfType<AmbienceManager>();
            }
            if (AmbienceManagerInstance != null)
                SetupGlobalSequenceReorderable();

            UpdateTabs();
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
#else
            EditorApplication.playmodeStateChanged -= OnPlayModeChanged;
            EditorApplication.playmodeStateChanged += OnPlayModeChanged;
#endif
#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged -= OnAssetsChanged;
            EditorApplication.projectChanged += OnAssetsChanged;
#else
            EditorApplication.projectWindowChanged -= OnAssetsChanged;
            EditorApplication.projectWindowChanged += OnAssetsChanged;
#endif
            AmbienceManager.OnValueChanged -= OnValueChanged;
            AmbienceManager.OnValueChanged += OnValueChanged;
            UpdateSpatializers();
        }

        void SetupGlobalSequenceReorderable() { 
            m_globalSequencesReorderable = new UnityEditorInternal.ReorderableList(AmbienceManagerInstance.m_globalSequences, typeof(Sequence), true, true, true, true);
            m_globalSequencesReorderable.elementHeight = EditorGUIUtility.singleLineHeight;
            m_globalSequencesReorderable.drawElementCallback = DrawSequenceElement;
            m_globalSequencesReorderable.drawHeaderCallback = DrawSequenceHeader;
            m_globalSequencesReorderable.onAddCallback = OnAddGlobalSequence;
            m_globalSequencesReorderable.onRemoveCallback = OnRemovedGlobalSequence;
        }
        /// <summary>
        /// Clears _allSequences, _allModifiers, and _allValues so they can be rebuilt if needed again.
        /// </summary>
#if UNITY_2017_1_OR_NEWER
        [UnityEditor.Callbacks.DidReloadScripts] //helps alieviate random issues but not required to run. won't work pre-2017
#endif
        static void OnAssetsChanged() {
            _allSequences = null;
            _allModifiers = null;
            UpdateSpatializers();
        }
        /// <summary> Called when Application PlayMode changes (Fixes issue where AnimBool looses reference to Repaint when exiting play mode) </summary>
        /// <param name="pmsc">How PlayMode changed</param>
        void OnPlayModeChanged(
#if UNITY_2017_2_OR_NEWER
        PlayModeStateChange pmsc
#endif
            ) {
            m_editorUtils.Dispose();
            m_editorUtils = PWApp.GetEditorUtils(this, UpdateTabs);
            Manager_UseAudioSourceGroup.valueChanged.RemoveAllListeners();
            Manager_UseAudioSourceGroup.valueChanged.AddListener(Repaint);
            UpdateTabs();
            Repaint();
        }
        /// <summary> Responds to Value changing during play by repainting window to display new Value position </summary>
        /// <param name="Name">Name of Value that changed</param>
        void OnValueChanged(string Name) {
            Repaint();
        }
        /// <summary> Destructor to release all created references and remove project events </summary>
        private void OnDestroy() {
            if(Instance == this)
                Instance = null;
            AmbienceManager.OnValueChanged -= OnValueChanged;
#if UNITY_2017_2_OR_NEWER
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#else
            EditorApplication.playmodeStateChanged -= OnPlayModeChanged;
#endif
#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged -= OnAssetsChanged;
#else
            EditorApplication.projectWindowChanged -= OnAssetsChanged;
#endif
            if (m_editorUtils != null)
            {
                m_editorUtils.Dispose();
            }
        }
        /// <summary> Updates current Spatializer and list of all available ones </summary>
        static void UpdateSpatializers() {
#if UNITY_2017_2_OR_NEWER
            string curAudioSpatializer = AudioSettings.GetSpatializerPluginName();
            allSpatializers = new List<string>(AudioSettings.GetSpatializerPluginNames());
            allSpatializers.Insert(0, "(None)");
            hasMicrosoftSpatializer = false;
            hasOculusSpatializer = false;
            for (int s = 0; s < allSpatializers.Count; ++s) {
                if (allSpatializers[s] == "MS HRTF Spatializer")
                    hasMicrosoftSpatializer = true;
                if (allSpatializers[s] == "OculusSpatializer")
                    hasOculusSpatializer = true;
                if (allSpatializers[s] == curAudioSpatializer)
                    curSpatializerPluginIdx = s;
            }
#endif
        }
#endregion

#region GUI main
        /// <summary> Main GUI function </summary>
        void OnGUI() {
            if (wantsOpenAssets) {
                wantsOpenAssets = false;
                var win = GetWindow<AssetWindowEditor>(false, "Assets");
                win.Show();
            }
            Instance = this;
            m_editorUtils.Initialize(); // Do not remove this!
            m_editorUtils.GUIHeader();
            
            if (AmbienceManagerInstance == null) {
                AmbienceManagerInstance = FindObjectOfType<AmbienceManager>();
                if (AmbienceManagerInstance == null) {
                    m_editorUtils.Text("CreateAmbienceManagerText");
                    if (m_editorUtils.Button("CreateAmbienceManagerButton")) {
                        AudioListener al = FindObjectOfType<AudioListener>();
                        if (al) {
                            AmbienceManagerInstance = al.gameObject.AddComponent<AmbienceManager>();
                            AmbienceManagerInstance.m_playerObject = Camera.main.transform;
                        } else {
                            GUIContent dialog = m_editorUtils.GetContent("CreateAmbienceManagerErrorDialog");
                            EditorUtility.DisplayDialog(dialog.text, dialog.tooltip, m_editorUtils.GetContent("OKButton").text);
                        }
                    }
                    //m_editorUtils.GUIFooter();
                    return;
                }
            }
            if(m_globalSequencesReorderable == null)
                SetupGlobalSequenceReorderable();
            m_globalSequencesReorderable.list = AmbienceManagerInstance.m_globalSequences;
#if UNITY_2017_2_OR_NEWER
            if (allSpatializers == null || allSpatializers.Count <= 1)
                UpdateSpatializers();
#endif
            m_editorUtils.Panel("ProjectSettingsPanel", ProjectSettingsPanel, curSpatializerPluginIdx == 0);
            m_editorUtils.Panel("ManagerSettingsPanel", ManagerSettingsPanel, true);
            m_editorUtils.Tabs(mainTabs);
            //m_editorUtils.GUIFooter();
            EditorGUILayout.Space();
        }
#endregion
#region Tabs
        void ManagerSettingsPanel(bool inlineHelp) {
            if (Manager_UseAudioSourceGroup == null) {
                Manager_UseAudioSourceGroup = new AnimBool(AmbienceManagerInstance.m_useAudioSource);
                Manager_UseAudioSourceGroup.valueChanged.AddListener(Repaint);
            }
            AmbienceManagerInstance.m_playerObject = m_editorUtils.ObjectField("Manager_PlayerObject", AmbienceManagerInstance.m_playerObject, typeof(Transform), true, inlineHelp) as Transform;
            AmbienceManagerInstance.m_volume = m_editorUtils.Slider("Manager_Volume", AmbienceManagerInstance.m_volume, 0f, 1f, inlineHelp);
            AmbienceManagerInstance.m_playSpeed = m_editorUtils.FloatField("Manager_PlaySpeed", AmbienceManagerInstance.m_playSpeed, inlineHelp);
            AmbienceManager.s_preloadAudio = AmbienceManagerInstance.m_preloadAudio = m_editorUtils.Toggle("Manager_PreloadAudio", AmbienceManagerInstance.m_preloadAudio, inlineHelp);
            AmbienceManagerInstance.m_useAudioSource = m_editorUtils.Toggle("Manager_UseAudioSource", AmbienceManagerInstance.m_useAudioSource, inlineHelp);
            Manager_UseAudioSourceGroup.target = AmbienceManagerInstance.m_useAudioSource;
            if (EditorGUILayout.BeginFadeGroup(Manager_UseAudioSourceGroup.faded)) {
                AmbienceManagerInstance.m_audioSourcePrefab = m_editorUtils.ObjectField("Manager_AudioSourcePrefab", AmbienceManagerInstance.m_audioSourcePrefab, typeof(GameObject), false, inlineHelp) as GameObject;
                AmbienceManagerInstance.m_audioSourceChannels = m_editorUtils.IntField("Manager_AudioSourceChannels", AmbienceManagerInstance.m_audioSourceChannels, inlineHelp);
            }
            EditorGUILayout.EndFadeGroup();
        }
        void ProjectSettingsPanel(bool inlineHelp) {
#if UNITY_2017_2_OR_NEWER
            string curAudioSpatializer = AudioSettings.GetSpatializerPluginName();
            if (curAudioSpatializer == "") {
                string message = m_editorUtils.GetContent("NoAudioSpatializerMessage").text;
                if (!hasMicrosoftSpatializer && !hasOculusSpatializer) {
                    message += m_editorUtils.GetContent("NoAudioSpatializerMissingBoth").text;
                } else if (!hasMicrosoftSpatializer) {
                    message += m_editorUtils.GetContent("NoAudioSpatializerMissingMicrosoft").text;
                } else if (!hasOculusSpatializer) {
                    message += m_editorUtils.GetContent("NoAudioSpatializerMissingOculus").text;
                }
                EditorGUILayout.HelpBox(message, MessageType.Warning, true);
            }
            EditorGUI.BeginChangeCheck();
            GUIContent[] spatializerOptions = new GUIContent[allSpatializers.Count];
            for (int o = 0; o < allSpatializers.Count; ++o)
                spatializerOptions[o] = new GUIContent(allSpatializers[o]);
            curSpatializerPluginIdx = EditorGUILayout.Popup(m_editorUtils.GetContent("SpatializerPlugin"), curSpatializerPluginIdx, spatializerOptions);
            if (EditorGUI.EndChangeCheck()) {
                string newSpat = allSpatializers[curSpatializerPluginIdx];
                if (newSpat == "(None)")
                    newSpat = "";
                AudioSettings.SetSpatializerPluginName(newSpat);
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty(); //mark active scenes dirty to tell unity it needs to save this. (apparently changing the spatializer doesn't mark anything dirty anymore)
            }
            m_editorUtils.InlineHelp("SpatializerPlugin", inlineHelp);
#else
            EditorGUILayout.HelpBox(m_editorUtils.GetContent("Unity2017.1SpatializerNote").text, MessageType.Info);
#endif
        }
        /// <summary> Updates TabSet when either window opens or language changes </summary>
        void UpdateTabs() {
            if (m_editorUtils != null) {
                List<Tab> newTabs = new List<Tab>() {
                    new Tab("SequencesTab", SequencesTab),
                    new Tab("ValuesTab", ValuesTab),
                    new Tab("EventsTab", EventsTab),
                    new Tab("SyncGroupsTab", SyncGroupsTab),
                };
                if (Application.isPlaying)
                    newTabs.Insert(0, new Tab("MonitorTab", MonitorTab));
                mainTabs = new TabSet(m_editorUtils, newTabs.ToArray());

            } else {
                mainTabs = new TabSet(m_editorUtils, new Tab[] {
                    new Tab(new GUIContent("Error"), ErrorTab),
                });
            }
        }
        /// <summary> "Sequences" tab that contains a list of all assets (if AssetWindow is not open) and all AudioArea in scene and Global Sequences on AmbienceManager </summary>
        void SequencesTab() {
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
            if (!AssetWindowEditor.IsOpen) {
                EditorGUILayout.BeginHorizontal();
                m_editorUtils.Heading("AssetsHeading");
                GUILayout.FlexibleSpace();
                if (m_editorUtils.ButtonNonLocalized(">>")) {
                    wantsOpenAssets = true;
                }
                EditorGUILayout.EndHorizontal();
                m_editorUtils.Panel("SequencesPanel", SequencesPanel, true);
                m_editorUtils.Panel("ModifiersPanel", ModifiersPanel, true);
            }
            m_editorUtils.Heading("SequencesHeading");
            m_editorUtils.Panel("AudioAreaPanel", AudioAreaPanel, true);
            m_editorUtils.Panel("GlobalSequencesPanel", GlobalSequencesPanel, true);
            GUILayout.EndScrollView();
        }
        /// <summary> Displays a list of all Sequence assets in project </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void SequencesPanel(bool inlineHelp) {
            Event currentEvent = Event.current;
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
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
                if (r.yMax >= m_scrollPosition.y) {
                    if (currentEvent.type == EventType.MouseDown && r.Contains(currentEvent.mousePosition)) {
                        lastClickedOn = data;
                    } else if (currentEvent.type == EventType.MouseDrag && lastClickedOn == data && r.Contains(currentEvent.mousePosition)) {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { data };
                        DragAndDrop.StartDrag("Sequence:" + data.name);
                        currentEvent.Use();
                        weStartedDrag = true;
                    } else if (currentEvent.type == EventType.DragUpdated && r.Contains(currentEvent.mousePosition)) {
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
                    } else if (currentEvent.type == EventType.DragPerform && r.Contains(currentEvent.mousePosition)) {
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
                    } else if ((currentEvent.type == EventType.DragExited || currentEvent.type == EventType.MouseUp) && weStartedDrag) {
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
            if (currentEvent.type == EventType.DragUpdated && buttonRect.Contains(currentEvent.mousePosition)) {
                for (int o = 0; o < DragAndDrop.objectReferences.Length; ++o) {
                    if (DragAndDrop.objectReferences[o] is AudioClip) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                        break;
                    }
                }
            } else if (currentEvent.type == EventType.DragPerform && buttonRect.Contains(currentEvent.mousePosition)) {
                List<Sequence.ClipData> clips = new List<Sequence.ClipData>();
                for (int o = 0; o < DragAndDrop.objectReferences.Length; ++o)
                    if (DragAndDrop.objectReferences[o] is AudioClip) 
                        clips.Add(DragAndDrop.objectReferences[o] as AudioClip);
                if (clips.Count > 0) {
                    if (currentEvent.control) { //create one for each
                        GUIContent dialogContent = m_editorUtils.GetContent("SequenceMultiCreateDialog");
                        string saveFolder = EditorUtility.SaveFolderPanel(dialogContent.text, "Assets", "Assets");
                        if (!string.IsNullOrEmpty(saveFolder)) {
                            if (saveFolder.StartsWith(Application.dataPath)) {
                                saveFolder = saveFolder.Substring(Application.dataPath.Length-6);
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
            Event currentEvent = Event.current;
            foreach (Modifier mod in AllModifiers) {
                Rect r = EditorGUILayout.GetControlRect();
                if (r.yMax > m_scrollPosition.y) {
                    if (currentEvent.type == EventType.MouseDown && r.Contains(currentEvent.mousePosition)) {
                        lastClickedOn = mod;
                    } else if (currentEvent.type == EventType.MouseDrag && lastClickedOn == mod && r.Contains(currentEvent.mousePosition)) {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] { mod };
                        DragAndDrop.StartDrag("Modifier:" + mod.name);
                        currentEvent.Use();
                        weStartedDrag = true;
                    } else if ((currentEvent.type == EventType.DragExited || currentEvent.type == EventType.MouseUp) && weStartedDrag) {
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
        /// <summary> Displays a list of all AudioAreas in scene with name editing and delete button </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void AudioAreaPanel(bool inlineHelp) {
            Event currentEvent = Event.current;
            AudioArea[] allAreas = FindObjectsOfType<AudioArea>();
            AudioArea toDelete = null;
            foreach (AudioArea ps in allAreas) {
                EditorGUILayout.BeginHorizontal();
                Rect r = EditorGUILayout.GetControlRect();
                if (currentEvent.type == EventType.MouseDown && r.Contains(currentEvent.mousePosition)) {
                    Selection.activeGameObject = ps.gameObject;
                }
                ps.name = EditorGUI.TextField(r, ps.name);
                if (m_editorUtils.Button("AudioAreaDeleteButton", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
                    toDelete = ps;
                EditorGUILayout.EndHorizontal();
            }
            ++EditorGUI.indentLevel;
            if (toDelete != null) {
                if (toDelete.transform.childCount > 0 || toDelete.GetComponents<Component>().Length > 2)
                    DestroyImmediate(toDelete); //just remove the AudioArea component if other stuff has been added to this GameObject
                else
                    DestroyImmediate(toDelete.gameObject); //nothing else has been added so delete the whole object
                GUI.FocusControl(""); //fixes bug where selected Text of one item would remain in old position until another control is focused
            }
            EditorGUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            //     if (m_editorUtils.Button("AudioAreaAddButton", EditorStyles.miniButton)) {
            if (m_editorUtils.Button("AudioAreaAddButton", GUILayout.ExpandWidth(false)))
            {
                GUI.FocusControl(""); //fixes bug where selected Text of one item would remain in old position until another control is focused
                Selection.activeGameObject = new GameObject("New AudioArea", new System.Type[] { typeof(AudioArea) });
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_editorUtils.InlineHelp("AudioAreaAddButton", inlineHelp);
            EditorGUILayout.EndHorizontal();
            --EditorGUI.indentLevel;
        }
        void DrawSequenceElement(Rect rect, int index, bool isActive, bool isFocused) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            AmbienceManagerInstance.m_globalSequences[index] = EditorGUI.ObjectField(rect, GUIContent.none, AmbienceManagerInstance.m_globalSequences[index], typeof(Sequence), false) as Sequence;
            EditorGUI.indentLevel = oldIndent;
        }
        void DrawSequenceHeader(Rect rect) {
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.LabelField(rect, PropertyCount("GlobalSequencesPanel", AmbienceManagerInstance.m_globalSequences));
            EditorGUI.indentLevel = oldIndent;
        }
        void OnAddGlobalSequence(UnityEditorInternal.ReorderableList list) {
            Sequence[] newList = new Sequence[AmbienceManagerInstance.m_globalSequences.Length + 1];
            for (int i = 0; i < AmbienceManagerInstance.m_globalSequences.Length; ++i) {
                newList[i] = AmbienceManagerInstance.m_globalSequences[i];
            }
            AmbienceManagerInstance.m_globalSequences = newList;
            list.list = AmbienceManagerInstance.m_globalSequences;
        }
        void OnRemovedGlobalSequence(UnityEditorInternal.ReorderableList list) {
            int idx = list.index;
            if (idx < 0 || idx >= AmbienceManagerInstance.m_globalSequences.Length)
                return;
            Sequence toRemove = AmbienceManagerInstance.m_globalSequences[idx];
            Sequence[] newList = new Sequence[AmbienceManagerInstance.m_globalSequences.Length - 1];
            for (int i = 0; i < newList.Length; ++i) {
                if (i < idx) {
                    newList[i] = AmbienceManagerInstance.m_globalSequences[i];
                } else if (i >= idx) {
                    newList[i] = AmbienceManagerInstance.m_globalSequences[i + 1];
                }
            }
            AmbienceManagerInstance.m_globalSequences = newList;
            m_globalSequencesReorderable.list = AmbienceManagerInstance.m_globalSequences;
            if (Application.isPlaying && toRemove != null && !AmbienceManager.WasSequenceAdded(toRemove))
                AmbienceManager.OnEditorRemovedSequence(toRemove);
        }
        private GUIContent PropertyCount<T>(string key, IList<T> list) {
            GUIContent content = m_editorUtils.GetContent(key);
            content.text += " [" + list.Count + "]";
            return content;
        }
        /// <summary> Displays a list of all Global Sequences on AmbienceManager </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void GlobalSequencesPanel(bool inlineHelp) {
            Rect r = EditorGUILayout.GetControlRect(false, m_globalSequencesReorderable.GetHeight());
            m_globalSequencesReorderable.DoList(r);
            if (Event.current.type == EventType.DragUpdated) {
                bool isValid = false;
                for (int i = 0; i < DragAndDrop.objectReferences.Length; ++i) {
                    if (DragAndDrop.objectReferences[i] is Sequence) {
                        isValid = true;
                        break;
                    }
                }
                DragAndDrop.visualMode = isValid ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
            } else if (Event.current.type == EventType.DragPerform) {
                List<Sequence> newList = new List<Sequence>(AmbienceManagerInstance.m_globalSequences);
                for (int i = 0; i < DragAndDrop.objectReferences.Length; ++i) {
                    if (DragAndDrop.objectReferences[i] is Sequence) {
                        newList.Add(DragAndDrop.objectReferences[i] as Sequence);
                    }
                }
                if (newList.Count != AmbienceManagerInstance.m_globalSequences.Length) {
                    AmbienceManagerInstance.m_globalSequences = newList.ToArray();
                    m_globalSequencesReorderable.list = AmbienceManagerInstance.m_globalSequences;
                }
            }
        }

        /// <summary> "Values" tab that displays timeline-style view of all Values found on Sequence or Modifier assets </summary>
        void ValuesTab() {
            //clear the data if we just pressed the mouse button in the window (prevents last drag that ended outside of window thinking this frame is the MouseUp frame and getting the wrong position)
            if (Event.current.type == EventType.MouseDown) {
                curValueEditing = null;
                curValueEditingType = 0;
                curValueEditingStartPos = 0f;
                curValueEditingStartMin = 0f;
                curValueEditingStartMax = 0f;
            }
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
            if (valuesHelpActive) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                m_editorUtils.HelpToggle(ref valuesHelpActive);
                EditorGUILayout.EndHorizontal();
                m_editorUtils.InlineHelp("ValuesTab", valuesHelpActive);
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(m_editorUtils.GetContent("ValueGuidelineCount"));
            string countVal = ValueGuidelineCount.ToString();
            EditorGUI.BeginChangeCheck();
            countVal = GUILayout.TextField(countVal, GUILayout.MinWidth(60f));
            if (EditorGUI.EndChangeCheck()) {
                int v;
                if (int.TryParse(countVal, out v)) {
                    ValueGuidelineCount = v;
                } else {
                    ValueGuidelineCount = 0;
                }
            }
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("ValueGuidelineCount", valuesHelpActive);
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            ValueGuidelineSnap = GUILayout.Toggle(ValueGuidelineSnap, m_editorUtils.GetContent("ValueGuidelineSnap"));
            m_editorUtils.InlineHelp("ValueGuidelineSnap", valuesHelpActive);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (!valuesHelpActive)
                m_editorUtils.HelpToggle(ref valuesHelpActive);
            EditorGUILayout.EndHorizontal();
            List<AssetInfo> allValues = GetAllValues();
            if (allValues.Count == 0)
                m_editorUtils.Label("ValuesEmptyMessage", new GUIStyle(EditorStyles.boldLabel) { wordWrap = true });
            else
                foreach (AssetInfo ai in allValues)
                    DrawValueRow(ai);
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            DrawValueDropArea();
        }
        /// <summary> Displays a single Value and all Sequences or Modifiers that reference it </summary>
        /// <param name="valueInfo">AssetInfo for Value to draw</param>
        void DrawValueRow(AssetInfo valueInfo) {
#region Setup
            Event currentEvent = Event.current;
            int totalCount = 0;
            foreach (Sequence seq in valueInfo.Sequences)
                foreach (SliderRange s in seq.m_values)
                    if (s.m_name == valueInfo.Name)
                        ++totalCount;
            foreach (Modifier mod in valueInfo.Modifiers)
                foreach (SliderRange s in mod.m_values)
                    if (s.m_name == valueInfo.Name)
                        ++totalCount;
            float totalHeight = Mathf.Max(EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight * (totalCount) + EditorGUIUtility.standardVerticalSpacing * totalCount - 1);
            Rect labelRect = EditorGUILayout.GetControlRect(true);
            Rect mainRect = EditorGUILayout.GetControlRect(true, totalHeight);
            float yPos = mainRect.y;
#endregion
#region Name Editable Label
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.TextField(labelRect, valueInfo.Name);
            if (string.IsNullOrEmpty(valueInfo.Name)) {
                EditorGUI.LabelField(labelRect, m_editorUtils.GetTextValue("ValueNameLabel"), EditorStyles.centeredGreyMiniLabel);
            }
            if (EditorGUI.EndChangeCheck()) {
                foreach (Sequence seq in valueInfo.Sequences) {
                    foreach (SliderRange s in seq.m_values)
                        if (s.m_name == valueInfo.Name)
                            s.m_name = newName;
                }
                foreach (Modifier mod in valueInfo.Modifiers) {
                    foreach (SliderRange s in mod.m_values)
                        if (s.m_name == valueInfo.Name)
                            s.m_name = newName;
                }
                valueInfo.Name = newName;
            }
#endregion
#region Drag And Drop
            //check for DragAndDrop content
            if (currentEvent.type == EventType.DragUpdated && mainRect.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Modifier || DragAndDrop.objectReferences[x] is Sequence) {
                        foundOne = true;
                        break;
                    }
                }
                if (foundOne)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            } else if (currentEvent.type == EventType.DragPerform && mainRect.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Modifier) {
                        foundOne = true;
                        Modifier mod = DragAndDrop.objectReferences[x] as Modifier;
                        SliderRange[] newValues = new SliderRange[mod.m_values.Length + 1];
                        mod.m_values.CopyTo(newValues, 0);
                        SliderRange newValue = new SliderRange();
                        newValue.m_name = valueInfo.Name;
                        newValues[newValues.Length - 1] = newValue;
                        mod.m_values = newValues;
                        mod.m_requirements |= ValuesOrEvents.Values;
                    } else if (DragAndDrop.objectReferences[x] is Sequence) {
                        foundOne = true;
                        Sequence seq = DragAndDrop.objectReferences[x] as Sequence;
                        SliderRange[] newValues = new SliderRange[seq.m_values.Length + 1];
                        seq.m_values.CopyTo(newValues, 0);
                        SliderRange newValue = new SliderRange();
                        newValue.m_name = valueInfo.Name;
                        newValues[newValues.Length - 1] = newValue;
                        seq.m_values = newValues;
                        seq.m_requirements |= ValuesOrEvents.Values;
                    }
                }
                if (foundOne) {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                }
            }
#endregion
#region Draw Value Tracks
            EditorGUI.DrawRect(mainRect, new Color(0.27f, 0.27f, 0.27f, 1f));
            float totalWidth = mainRect.width;
            //draw horizontal dividers between sequences
            for (int s = 1; s < totalCount; ++s) {
                Rect dividerRect = new Rect(mainRect.x, mainRect.y + s * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) - EditorGUIUtility.standardVerticalSpacing / 2f, totalWidth, 1f);
                EditorGUI.DrawRect(dividerRect, new Color(0f, 0f, 0f, 0.75f));
            }
            //draw vertical dividers along Value
            for (int x = 0; x <= ValueGuidelineCount; ++x) {
                Rect dividerRect = new Rect(mainRect.x + (x + 1) * totalWidth / (ValueGuidelineCount + 1), mainRect.y, 1f, mainRect.height);
                Color dividerColour = new Color(1f, 1f, 1f, 0.3f);
                if (ValueGuidelineCount % 2 == 1 && x % 2 == 1) //if we have an even number of dividers, make every odd position one lighter
                    dividerColour.a = 0.15f;
                EditorGUI.DrawRect(dividerRect, dividerColour);
            }
#endregion
#region Sequences
            for (int s = 0; s < valueInfo.Sequences.Count; ++s) {
                Sequence data = valueInfo.Sequences[s];
                for (int x = 0; x < data.m_values.Length; ++x) {
                    if (data.m_values[x].m_name == valueInfo.Name) {
                        SliderRange value = data.m_values[x];
                        Rect valueRect = new Rect(mainRect.x, yPos, totalWidth, EditorGUIUtility.singleLineHeight);
                        DrawValueElement(valueRect, new Color(0f, 0.5f, 0f, 1f), value, data.name + " (Sequence)", data);
                        yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }
#endregion
#region Modifiers
            for (int s = 0; s < valueInfo.Modifiers.Count; ++s) {
                Modifier mod = valueInfo.Modifiers[s];
                for (int x = 0; x < mod.m_values.Length; ++x) {
                    if (mod.m_values[x].m_name == valueInfo.Name) {
                        SliderRange value = mod.m_values[x];
                        Rect valueRect = new Rect(mainRect.x, yPos, totalWidth, EditorGUIUtility.singleLineHeight);
                        DrawValueElement(valueRect, new Color(0.5f, 0.5f, 0f, 1f), value, mod.name + " (Modifier)", mod);
                        yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }
#endregion
#region Draw 'Current Position' Marker
            if (Application.isPlaying) {
                Rect dividerRect = new Rect(mainRect.x + AmbienceManager.GetValue(valueInfo.Name) * totalWidth, mainRect.y, 1f, mainRect.height);
                EditorGUI.DrawRect(dividerRect, Color.white);
            }
#endregion
        }
        /// <summary> Displays a single reference to a Value and allows editing of ranges </summary>
        /// <param name="valueRect">Rect to display this element</param>
        /// <param name="drawColor">Color to display this element as</param>
        /// <param name="value">SliderRange to display/edit</param>
        /// <param name="displayName">Name of Sequence or Modifier that contains this Value</param>
        /// <param name="parent">Reference to Sequence or Modifier that contains this Value</param>
        void DrawValueElement(Rect valueRect, Color drawColor, SliderRange value, string displayName, Object parent) {
            Color originalGUIColor = GUI.color;
            Event currentEvent = Event.current;
            float totalWidth = valueRect.width;
            float totalHeight = valueRect.height;
            Rect drawRect = new Rect(valueRect.x + totalWidth * value.m_min, valueRect.y, totalWidth * (value.m_max - value.m_min), totalHeight);
            Rect falloffLeftRect = new Rect(drawRect.xMin - value.m_minFalloff * totalWidth, drawRect.y, value.m_minFalloff * totalWidth, drawRect.height);
            Rect falloffRightRect = new Rect(drawRect.xMax, drawRect.y, value.m_maxFalloff * totalWidth, drawRect.height);
            //Default inspector-style click to ping, double-click to select
            if (parent != null && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && drawRect.Contains(currentEvent.mousePosition)) {
                if (currentEvent.clickCount == 1)
                    EditorGUIUtility.PingObject(parent);
                else
                    Selection.activeInstanceID = parent.GetInstanceID();
            }
            //Check for Events
            Rect valuePanRect = new Rect(drawRect.xMin + 5f, drawRect.y, drawRect.width - 10f, totalHeight);
            Rect valueEndRect = new Rect(drawRect.xMax - 5f, drawRect.y, 10f, totalHeight);
            Rect valueBeginRect = new Rect(drawRect.xMin - 5f, drawRect.y, 10f, totalHeight);
            if (curValueEditing == null) {
                EditorGUIUtility.AddCursorRect(valuePanRect, MouseCursor.MoveArrow);
                EditorGUIUtility.AddCursorRect(valueEndRect, MouseCursor.SplitResizeLeftRight);
                EditorGUIUtility.AddCursorRect(valueBeginRect, MouseCursor.SplitResizeLeftRight);
                if (value.m_min > 0f && value.m_minFalloff > 0f)
                    EditorGUIUtility.AddCursorRect(falloffLeftRect, MouseCursor.SlideArrow);
                if (value.m_max < 1f && value.m_maxFalloff > 0f)
                    EditorGUIUtility.AddCursorRect(falloffRightRect, MouseCursor.SlideArrow);
            } else {
                switch (curValueEditingType) {
                    case 1:
                        EditorGUIUtility.AddCursorRect(valueRect, MouseCursor.MoveArrow);
                        break;
                    case 2:
                    case 3:
                        EditorGUIUtility.AddCursorRect(valueRect, MouseCursor.SplitResizeLeftRight);
                        break;
                    case 4:
                    case 5:
                        EditorGUIUtility.AddCursorRect(valueRect, MouseCursor.SlideArrow);
                        break;
                }
            }

            //check for drag of either ends or Value
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1 && valueRect.Contains(currentEvent.mousePosition)) {
                GenericMenu contextMenu = new GenericMenu();
                ValueContextInfo contextInfo = new ValueContextInfo(value, parent);
                contextMenu.AddItem(m_editorUtils.GetContent("ContextMenuInvert"), false, DoContextMenuInvert, contextInfo);
                contextMenu.AddItem(m_editorUtils.GetContent("ContextMenuCopy"), false, DoContextMenuCopy, contextInfo);
                if (EditorGUIUtility.systemCopyBuffer == null || !EditorGUIUtility.systemCopyBuffer.StartsWith("Value:"))
                    contextMenu.AddDisabledItem(m_editorUtils.GetContent("ContextMenuPaste"));
                else
                    contextMenu.AddItem(m_editorUtils.GetContent("ContextMenuPaste"), false, DoContextMenuPaste, contextInfo);
                contextMenu.AddSeparator("");
                contextMenu.AddItem(m_editorUtils.GetContent("ContextMenuDelete"), false, DoContextMenuDelete, contextInfo);
                contextMenu.ShowAsContext();
            } else if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && curValueEditing == null) {
                if (valueEndRect.Contains(currentEvent.mousePosition)) {
                    if (currentEvent.control) {
                        curValueEditingType = 5;
                        curValueEditingStartMax = value.m_maxFalloff;
                    } else {
                        curValueEditingType = 3;
                        curValueEditingStartMax = value.m_max;
                    }
                    curValueEditing = value;
                    curValueEditingStartPos = currentEvent.mousePosition.x;
                    currentEvent.Use();
                } else if (valueBeginRect.Contains(currentEvent.mousePosition)) {
                    if (currentEvent.control) {
                        curValueEditingType = 4;
                        curValueEditingStartMin = value.m_minFalloff;
                    } else {
                        curValueEditingType = 2;
                        curValueEditingStartMin = value.m_min;
                    }
                    curValueEditing = value;
                    curValueEditingStartPos = currentEvent.mousePosition.x;
                    currentEvent.Use();
                } else if (valuePanRect.Contains(currentEvent.mousePosition)) {
                    curValueEditing = value;
                    curValueEditingStartPos = currentEvent.mousePosition.x;
                    curValueEditingStartMin = value.m_min;
                    curValueEditingStartMax = value.m_max;
                    curValueEditingType = 1;
                    currentEvent.Use();
                } else if (falloffLeftRect.Contains(currentEvent.mousePosition)) {
                    curValueEditingType = 4;
                    curValueEditingStartMin = value.m_minFalloff;
                    curValueEditing = value;
                    curValueEditingStartPos = currentEvent.mousePosition.x;
                    currentEvent.Use();
                } else if (falloffRightRect.Contains(currentEvent.mousePosition)) {
                    curValueEditingType = 5;
                    curValueEditingStartMax = value.m_maxFalloff;
                    curValueEditing = value;
                    curValueEditingStartPos = currentEvent.mousePosition.x;
                    currentEvent.Use();
                }
            } else if (currentEvent.type == EventType.MouseDrag && curValueEditing == value) {
                float moveAmount = (currentEvent.mousePosition.x - curValueEditingStartPos) / totalWidth;
                float snapSize = 5f / totalWidth; //5 pixels either direction should work
                switch (curValueEditingType) {
                    case 1:
                        moveAmount = Mathf.Clamp(moveAmount, -curValueEditingStartMin, 1f - curValueEditingStartMax);
                        float newMinVal = Mathf.Clamp(curValueEditingStartMin + moveAmount, 0f, 1f);
                        float newMaxVal = Mathf.Clamp(curValueEditingStartMax + moveAmount, 0f, 1f);
                        float snap = CheckValueSnap(newMinVal, snapSize);
                        if (snap == 0)
                            snap = CheckValueSnap(newMaxVal, snapSize);
                        value.m_min = snap + newMinVal;
                        value.m_max = snap + newMaxVal;
                        break;
                    case 2:
                        float newMin = Mathf.Clamp(curValueEditingStartMin + moveAmount, 0f, value.m_max);
                        value.m_min = CheckValueSnap(newMin, snapSize) + newMin;
                        break;
                    case 3:
                        float newMax = Mathf.Clamp(curValueEditingStartMax + moveAmount, value.m_min, 1f);
                        value.m_max = CheckValueSnap(newMax, snapSize) + newMax;
                        break;
                    case 4:
                        float newMinFalloff = Mathf.Clamp01(curValueEditingStartMin - moveAmount);
                        value.m_minFalloff = CheckValueSnap(newMinFalloff, snapSize) + newMinFalloff;
                        break;
                    case 5:
                        float newMaxFalloff = Mathf.Clamp01(curValueEditingStartMax + moveAmount);
                        value.m_maxFalloff = CheckValueSnap(newMaxFalloff, snapSize) + newMaxFalloff;
                        break;
                    default:
                        break;
                }
                currentEvent.Use();
            } else if (currentEvent.type == EventType.MouseUp) {
                curValueEditing = null;
                curValueEditingType = 0;
                curValueEditingStartPos = 0f;
                curValueEditingStartMin = 0f;
                curValueEditingStartMax = 0f;
                currentEvent.Use();
            }
            //Draw Value
            if (drawRect.width < 6f) {
                drawRect.xMin -= 3f;
                drawRect.xMax += 3f;
            }
            if (!value.m_invert) {
                Color falloffColor = drawColor;
                falloffColor.r += 0.1f;
                falloffColor.g += 0.1f;
                falloffColor.b += 0.1f;
                GUI.color = falloffColor;
                Rect FalloffTexCoords = new Rect(0,0,1,1);
                if (value.m_minFalloff > 0f) {
                    if (falloffLeftRect.xMin < valueRect.xMin) {
                        FalloffTexCoords.xMin = (valueRect.xMin - falloffLeftRect.xMin) / falloffLeftRect.width;
                        falloffLeftRect.xMin = valueRect.xMin;
                    } else
                        FalloffTexCoords.xMin = 0f;
                    GUI.DrawTextureWithTexCoords(falloffLeftRect, LeftFalloffTexture, FalloffTexCoords);
                    FalloffTexCoords.xMin = 0f;
                }
                if (value.m_maxFalloff > 0f) {
                    if (falloffRightRect.xMax > valueRect.xMax) {
                        FalloffTexCoords.xMax = 1f - (falloffRightRect.xMax - valueRect.xMax) / falloffRightRect.width;
                        falloffRightRect.xMax = valueRect.xMax;
                    } else
                        FalloffTexCoords.xMax = 1f;
                    GUI.DrawTextureWithTexCoords(falloffRightRect, RightFalloffTexture, FalloffTexCoords);
                }
                GUI.color = drawColor;
                EditorGUI.DrawRect(drawRect, Color.white);
                GUI.color = originalGUIColor;
            } else {
                GUI.color = drawColor;
                Rect FalloffTexCoords = new Rect(0, 0, 1, 1);
                if (value.m_minFalloff > 0f) {
                    if (falloffLeftRect.xMin < valueRect.xMin) {
                        FalloffTexCoords.xMin = (valueRect.xMin - falloffLeftRect.xMin) / falloffLeftRect.width;
                        falloffLeftRect.xMin = valueRect.xMin;
                    } else
                        FalloffTexCoords.xMin = 0f;
                    GUI.DrawTextureWithTexCoords(falloffLeftRect, RightFalloffTexture, FalloffTexCoords);
                    FalloffTexCoords.xMin = 0f;
                }
                if (value.m_maxFalloff > 0f) {
                    if (falloffRightRect.xMax > valueRect.xMax) {
                        FalloffTexCoords.xMax = 1f - (falloffRightRect.xMax - valueRect.xMax) / falloffRightRect.width;
                        falloffRightRect.xMax = valueRect.xMax;
                    } else
                        FalloffTexCoords.xMax = 1f;
                    GUI.DrawTextureWithTexCoords(falloffRightRect, LeftFalloffTexture, FalloffTexCoords);
                }
                Rect drawRect1 = drawRect;
                Rect drawRect2 = drawRect;
                drawRect2.xMin = drawRect.xMax + falloffRightRect.width;
                drawRect2.xMax = valueRect.xMax;
                drawRect1.xMax = drawRect.xMin - falloffLeftRect.width;
                drawRect1.xMin = valueRect.xMin;
                EditorGUI.DrawRect(drawRect1, Color.white);
                EditorGUI.DrawRect(drawRect2, Color.white);
                GUI.color = originalGUIColor;
            }
            EditorGUI.LabelField(drawRect, displayName, EditorStyles.whiteMiniLabel);
        }
        float CheckValueSnap(float val, float snapSize) {
            if (!ValueGuidelineSnap)
                return 0f;
            float closestVal = val;
            float closestValDist = snapSize;
            for (int x = 0; x <= ValueGuidelineCount + 1; ++x) {
                float snap = x / ((float)ValueGuidelineCount + 1);
                float dist = Mathf.Abs(snap - val);
                if (dist < closestValDist) {
                    closestValDist = dist;
                    closestVal = snap;
                }
            }
            return closestVal - val;
        }
        /// <summary> Displays a box to drop Sequence or Modifier assets in to add a new Value </summary>
        void DrawValueDropArea() {
            Event currentEvent = Event.current;
            GUIContent dropBoxContent = m_editorUtils.GetContent("ValueDropArea");
            float minHeight = Mathf.Max(EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing, EditorSkin.centeredBody.CalcHeight(dropBoxContent, EditorGUIUtility.currentViewWidth));
            GUILayout.Box(GUIContent.none, EditorSkin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(minHeight));
            Rect r = GUILayoutUtility.GetLastRect();
            GUIStyle dropBoxStyle = new GUIStyle(EditorSkin.panelLabel);
            dropBoxStyle.alignment = TextAnchor.MiddleCenter;
            dropBoxStyle.wordWrap = true;
            GUI.Label(r, dropBoxContent, dropBoxStyle);
            if (currentEvent.type == EventType.DragUpdated && r.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Modifier || DragAndDrop.objectReferences[x] is Sequence) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        break;
                    }
                }
            } else if (currentEvent.type == EventType.DragPerform && r.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Modifier) {
                        foundOne = true;
                        Modifier mod = DragAndDrop.objectReferences[x] as Modifier;
                        SliderRange[] newValues = new SliderRange[mod.m_values.Length + 1];
                        mod.m_values.CopyTo(newValues, 0);
                        newValues[newValues.Length - 1] = new SliderRange();
                        mod.m_values = newValues;
                        mod.m_requirements |= ValuesOrEvents.Values;
                    } else if (DragAndDrop.objectReferences[x] is Sequence) {
                        foundOne = true;
                        Sequence seq = DragAndDrop.objectReferences[x] as Sequence;
                        SliderRange[] newValues = new SliderRange[seq.m_values.Length + 1];
                        seq.m_values.CopyTo(newValues, 0);
                        newValues[newValues.Length - 1] = new SliderRange();
                        seq.m_values = newValues;
                        seq.m_requirements |= ValuesOrEvents.Values;
                    }
                }
                if (foundOne) {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                }
            }
        }
        
        /// <summary> "Events" tab that displays a list of all Event names and which Sequences or Midifiers use them </summary>
        void EventsTab() {
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_editorUtils.HelpToggle(ref eventHelpActive);
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("EventsTab", eventHelpActive);
            List<AssetInfo> allEvents = GetAllEvents();
            if (allEvents.Count == 0) {
                m_editorUtils.Label("EventsEmptyMessage", new GUIStyle(EditorStyles.boldLabel) { wordWrap = true });
            } else
                foreach (AssetInfo ai in allEvents) {
                    DrawEventRow(ai);
                    EditorGUILayout.Space();
                }
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            DrawEventDropArea();
        }
        /// <summary> Displays a single "Event" and all Sequences or Modifiers that use it </summary>
        /// <param name="info">AssetInfo for Event to draw</param>
        void DrawEventRow(AssetInfo info) {
#region Setup
            Event currentEvent = Event.current;
            int totalCount = 0;
            foreach (Sequence seq in info.Sequences)
                foreach (string s in seq.m_events)
                    if (s == info.Name)
                        ++totalCount;
            foreach (Modifier mod in info.Modifiers)
                foreach (string s in mod.m_events)
                    if (s == info.Name)
                        ++totalCount;
            float totalHeight = Mathf.Max(EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight * (totalCount) + EditorGUIUtility.standardVerticalSpacing * (totalCount - 1));
            Rect labelRect = EditorGUILayout.GetControlRect(true);
            Rect mainRect = EditorGUILayout.GetControlRect(true, totalHeight);
            float yPos = mainRect.y;
#endregion
#region Name Editable Label
            Color startColor = GUI.color;
            if (Application.isPlaying) {
                bool isOn = AmbienceManager.GetEventActive(info.Name);
                if (isOn) {
                    GUI.color = new Color(0.2f, 0.8f, 0.2f, 1f);
                } else {
                    GUI.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                }
                /* indicator instead of color (left here just in case)
                Rect indicatorRect = new Rect(labelRect.xMin, labelRect.yMin, labelRect.height, labelRect.height);
                labelRect.xMin += labelRect.height;
                EditorGUI.LabelField(indicatorRect, EditorGUIUtility.IconContent("Main Light Gizmo"));
                if (currentEvent.type == EventType.MouseDown && indicatorRect.Contains(currentEvent.mousePosition)) {
                    if (isOn)
                        AmbienceManager.DeactivateEvent(info.Name);
                    else
                        AmbienceManager.ActivateEvent(info.Name);
                    currentEvent.Use();
                }
                //*/
            }
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.TextField(labelRect, info.Name);
            GUI.color = startColor;
            if (string.IsNullOrEmpty(info.Name)) {
                EditorGUI.LabelField(labelRect, m_editorUtils.GetTextValue("EventNameLabel"), EditorStyles.centeredGreyMiniLabel);
            }
            if (EditorGUI.EndChangeCheck()) {
                foreach (Sequence seq in info.Sequences) {
                    for(int e = 0; e < seq.m_events.Length; ++e) {
                        if (seq.m_events[e] == info.Name)
                            seq.m_events[e] = newName;
                    }
                }
                foreach (Modifier mod in info.Modifiers) {
                    for (int e = 0; e < mod.m_events.Length; ++e) {
                        if (mod.m_events[e] == info.Name)
                            mod.m_events[e] = newName;
                    }
                }
                info.Name = newName;
            }
#endregion
#region Drag And Drop
            //check for DragAndDrop content
            if (currentEvent.type == EventType.DragUpdated && mainRect.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Modifier) {
                        Modifier mod = DragAndDrop.objectReferences[x] as Modifier;
                        bool alreadyExists = false;
                        foreach (string e in mod.m_events) {
                            if (e == info.Name) {
                                alreadyExists = true;
                                break;
                            }
                        }
                        if (!alreadyExists) {
                            foundOne = true;
                            break;
                        }
                    } else if(DragAndDrop.objectReferences[x] is Sequence) {
                        Sequence seq = DragAndDrop.objectReferences[x] as Sequence;
                        bool alreadyExists = false;
                        foreach (string e in seq.m_events) {
                            if (e == info.Name) {
                                alreadyExists = true;
                                break;
                            }
                        }
                        if (!alreadyExists) {
                            foundOne = true;
                            break;
                        }
                    }
                }
                if (foundOne)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            } else if (currentEvent.type == EventType.DragPerform && mainRect.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Modifier) {
                        Modifier mod = DragAndDrop.objectReferences[x] as Modifier;
                        bool alreadyExists = false;
                        foreach (string e in mod.m_events) {
                            if (e == info.Name) {
                                alreadyExists = true;
                                break;
                            }
                        }
                        if (alreadyExists)
                            continue;
                        foundOne = true;
                        string[] newEvents = new string[mod.m_events.Length + 1];
                        mod.m_events.CopyTo(newEvents, 0);
                        newEvents[newEvents.Length - 1] = info.Name;
                        mod.m_events = newEvents;
                        mod.m_requirements |= ValuesOrEvents.Events;
                    } else if (DragAndDrop.objectReferences[x] is Sequence) {
                        Sequence seq = DragAndDrop.objectReferences[x] as Sequence;
                        bool alreadyExists = false;
                        foreach (string e in seq.m_events) {
                            if (e == info.Name) {
                                alreadyExists = true;
                                break;
                            }
                        }
                        if (alreadyExists)
                            continue;
                        foundOne = true;
                        string[] newEvents = new string[seq.m_events.Length + 1];
                        seq.m_events.CopyTo(newEvents, 0);
                        newEvents[newEvents.Length - 1] = info.Name;
                        seq.m_events = newEvents;
                        seq.m_requirements |= ValuesOrEvents.Events;
                    }
                }
                if (foundOne) {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                }
            }
#endregion
#region Sequences
            GUIContent removeButtonContent = m_editorUtils.GetContent("EventRemoveButton");
            float objectFieldWidth = mainRect.width - EditorStyles.miniButtonRight.CalcSize(removeButtonContent).x;
            foreach (Sequence seq in info.Sequences) {
                Rect r = new Rect(mainRect.x, yPos, objectFieldWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(r, seq, typeof(Sequence), false);
                r.xMin = r.xMax;
                r.xMax = mainRect.xMax;
                if (GUI.Button(r, removeButtonContent, EditorStyles.miniButtonRight)) {
                    List<string> newEvents = new List<string>(seq.m_events);
                    newEvents.RemoveAll(delegate (string s) {
                        return s == info.Name;
                    });
                    seq.m_events = newEvents.ToArray();
                    if (newEvents.Count == 0)
                        seq.m_requirements &= ~ValuesOrEvents.Events;
                }
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
#endregion
#region Modifiers
            foreach (Modifier mod in info.Modifiers) {
                Rect r = new Rect(mainRect.x, yPos, objectFieldWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(r, mod, typeof(Modifier), false);
                r.xMin = r.xMax;
                r.xMax = mainRect.xMax;
                if (GUI.Button(r, removeButtonContent, EditorStyles.miniButtonRight)) {
                    List<string> newEvents = new List<string>(mod.m_events);
                    newEvents.RemoveAll(delegate (string s) {
                        return s == info.Name;
                    });
                    mod.m_events = newEvents.ToArray();
                    if (newEvents.Count == 0)
                        mod.m_requirements &= ~ValuesOrEvents.Events;
                }
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
#endregion
        }
        /// <summary> Displays a box to drop Sequence or Modifier assets in to add a new Value </summary>
        void DrawEventDropArea() {
            Event currentEvent = Event.current;
            GUIContent dropBoxContent = m_editorUtils.GetContent("EventsDropArea");
            float minHeight = Mathf.Max(EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing, EditorSkin.centeredBody.CalcHeight(dropBoxContent, EditorGUIUtility.currentViewWidth));
            GUILayout.Box(GUIContent.none, EditorSkin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(minHeight));
            Rect r = GUILayoutUtility.GetLastRect();
            GUIStyle dropBoxStyle = new GUIStyle(EditorSkin.panelLabel);
            dropBoxStyle.alignment = TextAnchor.MiddleCenter;
            dropBoxStyle.wordWrap = true;
            GUI.Label(r, dropBoxContent, dropBoxStyle);
            if (currentEvent.type == EventType.DragUpdated && r.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Modifier || DragAndDrop.objectReferences[x] is Sequence) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        break;
                    }
                }
            } else if (currentEvent.type == EventType.DragPerform && r.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Modifier) {
                        foundOne = true;
                        Modifier mod = DragAndDrop.objectReferences[x] as Modifier;
                        string[] newEvents = new string[mod.m_events.Length + 1];
                        mod.m_values.CopyTo(newEvents, 0);
                        newEvents[newEvents.Length - 1] = "";
                        mod.m_events = newEvents;
                        mod.m_requirements |= ValuesOrEvents.Events;
                    } else if (DragAndDrop.objectReferences[x] is Sequence) {
                        foundOne = true;
                        Sequence seq = DragAndDrop.objectReferences[x] as Sequence;
                        string[] newEvents = new string[seq.m_events.Length + 1];
                        seq.m_values.CopyTo(newEvents, 0);
                        newEvents[newEvents.Length - 1] = "";
                        seq.m_events = newEvents;
                        seq.m_requirements |= ValuesOrEvents.Events;
                    }
                }
                if (foundOne) {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                }
            }
        }

        void SyncGroupsTab() {
            Dictionary<string, List<Sequence>> syncGroups = new Dictionary<string, List<Sequence>>();
            foreach (Sequence s in AllSequences) {
                if (!string.IsNullOrEmpty(s.m_syncGroup)) {
                    if (syncGroups.ContainsKey(s.m_syncGroup))
                        syncGroups[s.m_syncGroup].Add(s);
                    else
                        syncGroups.Add(s.m_syncGroup, new List<Sequence> { s });
                }
            }
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, false);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_editorUtils.HelpToggle(ref syncHelpActive);
            EditorGUILayout.EndHorizontal();
            m_editorUtils.InlineHelp("SyncGroupsTab", syncHelpActive);
            if (syncGroups.Count == 0)
                m_editorUtils.Label("SyncGroupsEmptyMessage", new GUIStyle(EditorStyles.boldLabel) { wordWrap = true });
            else
                foreach (KeyValuePair<string, List<Sequence>> sg in syncGroups) {
                    DrawSyncGroupRow(sg.Key, sg.Value);
                    EditorGUILayout.Space();
                }
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            DrawSyncGroupDropArea();
        }
        void DrawSyncGroupRow(string SyncGroupName, List<Sequence> sequences) {
#region Setup
            Event currentEvent = Event.current;
            string newGroupName = SyncGroupName;
            if (newGroupName == "--InvalidSyncGroupName--")
                newGroupName = "";
            float totalHeight = Mathf.Max(EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight * (sequences.Count) + EditorGUIUtility.standardVerticalSpacing * (sequences.Count - 1));
            Rect labelRect = EditorGUILayout.GetControlRect(true);
            Rect mainRect = EditorGUILayout.GetControlRect(true, totalHeight);
            float yPos = mainRect.y;
            GUIContent syncRepeatLabel = m_editorUtils.GetContent("SyncRepeatLabel");
            GUIContent syncSqueezeLabel = m_editorUtils.GetContent("SyncSqueezeLabel");
            GUIContent syncStretchLabel = m_editorUtils.GetContent("SyncStretchLabel");
            float syncRepeatWidth = EditorStyles.miniBoldLabel.CalcSize(syncRepeatLabel).x;
            float syncSqueezeWidth = EditorStyles.miniBoldLabel.CalcSize(syncSqueezeLabel).x;
            float syncStretchWidth = EditorStyles.miniBoldLabel.CalcSize(syncStretchLabel).x;
#endregion
#region Get Sequence Lengths
            float totalLength = 0;
            float maxLength = 0f;
            float minLength = -1;
            float maxFlexLength = 0f;
            for (int t = 0; t < sequences.Count; ++t) {
                float length = 0;
                for (int c = 0; c < sequences[t].m_clipData.Length; ++c)
                    if(sequences[t].m_clipData[c].m_clip != null)
                        length += sequences[t].m_clipData[c].m_clip.length;
                SyncType sType = sequences[t].m_syncType;
                if ((sType & SyncType.STRETCH) == 0 && (minLength > length || minLength < 0))
                    minLength = length;
                if ((sType & SyncType.SQUEEZE) > 0) {
                    if (maxFlexLength < length)
                        maxFlexLength = length;
                } else if (maxLength < length)
                    maxLength = length;
            }
            totalLength = Mathf.Max(minLength, maxLength == 0 ? maxFlexLength : maxLength);
#endregion
#region SyncType Labels
            Rect syncTypeLabelRect = labelRect;
            syncTypeLabelRect.xMax = syncTypeLabelRect.xMin + syncRepeatWidth;
            EditorGUI.LabelField(syncTypeLabelRect, syncRepeatLabel, EditorStyles.miniBoldLabel);
            syncTypeLabelRect.xMin = syncTypeLabelRect.xMax + 3;
            syncTypeLabelRect.xMax = syncTypeLabelRect.xMin + syncSqueezeWidth;
            EditorGUI.LabelField(syncTypeLabelRect, syncSqueezeLabel, EditorStyles.miniBoldLabel);
            syncTypeLabelRect.xMin = syncTypeLabelRect.xMax + 3;
            syncTypeLabelRect.xMax = syncTypeLabelRect.xMin + syncStretchWidth;
            EditorGUI.LabelField(syncTypeLabelRect, syncStretchLabel, EditorStyles.miniBoldLabel);
            labelRect.xMin = syncTypeLabelRect.xMax + 3;
#endregion
#region Group Loop Length
            GUIContent loopLengthLabel = m_editorUtils.GetContent("LoopLengthLabel");
            float timeleft = totalLength;
            int Minutes = (int)(timeleft / 60f);
            timeleft -= Minutes * 60;
            int Hours = Minutes / 60;
            Minutes -= Hours * 60;
            loopLengthLabel.text += " " + Hours + ":" + Minutes + ":" + timeleft.ToString("F3");
            float loopLengthWidth = EditorStyles.miniBoldLabel.CalcSize(loopLengthLabel).x;
            Rect loopLengthRect = new Rect(labelRect.xMax - loopLengthWidth, labelRect.y, loopLengthWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(loopLengthRect, loopLengthLabel, EditorStyles.miniBoldLabel);
            labelRect.xMax -= loopLengthWidth;
#endregion
#region Name Editable Label
            EditorGUI.BeginChangeCheck();
            newGroupName = EditorGUI.TextField(labelRect, newGroupName);
            if (string.IsNullOrEmpty(newGroupName)) {
                EditorGUI.LabelField(labelRect, m_editorUtils.GetTextValue("SyncGroupNameLabel"), EditorStyles.centeredGreyMiniLabel);
            }
            if (EditorGUI.EndChangeCheck()) {
                if (string.IsNullOrEmpty(newGroupName))
                    newGroupName = "--InvalidSyncGroupName--";
                foreach (Sequence seq in sequences) {
                    if (seq.m_syncGroup == SyncGroupName)
                        seq.m_syncGroup = newGroupName;
                }
                SyncGroupName = newGroupName;
            }
#endregion
#region Drag And Drop
            //check for DragAndDrop content
            if (currentEvent.type == EventType.DragUpdated && mainRect.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Sequence) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        break;
                    }
                }
            } else if (currentEvent.type == EventType.DragPerform && mainRect.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Sequence) {
                        Sequence seq = DragAndDrop.objectReferences[x] as Sequence;
                        seq.m_syncGroup = SyncGroupName;
                    }
                }
                if (foundOne) {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                }
            }
#endregion
#region Sequences
            GUIContent removeButtonContent = m_editorUtils.GetContent("SyncGroupRemoveButton");
            float objectFieldWidth = mainRect.width - EditorStyles.miniButtonRight.CalcSize(removeButtonContent).x - syncRepeatWidth - syncSqueezeWidth - syncStretchWidth - 9f;
            int colorOffset = 0;
            foreach (Sequence seq in sequences) {
#region Sync Type Toggles
                Rect toggleRect = new Rect(mainRect.x, yPos, syncRepeatWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.BeginChangeCheck();
                bool tVal = EditorGUI.Toggle(toggleRect, (seq.m_syncType & SyncType.REPEAT) > 0);
                if (EditorGUI.EndChangeCheck()) {
                    if (tVal)
                        seq.m_syncType |= SyncType.REPEAT;
                    else
                        seq.m_syncType &= ~SyncType.REPEAT;
                }
                toggleRect.xMin = toggleRect.xMax + 3f;
                toggleRect.xMax = toggleRect.xMin + syncSqueezeWidth;
                EditorGUI.BeginChangeCheck();
                tVal = EditorGUI.Toggle(toggleRect, (seq.m_syncType & SyncType.SQUEEZE) > 0);
                if (EditorGUI.EndChangeCheck()) {
                    if (tVal)
                        seq.m_syncType |= SyncType.SQUEEZE;
                    else
                        seq.m_syncType &= ~SyncType.SQUEEZE;
                }
                toggleRect.xMin = toggleRect.xMax + 3f;
                toggleRect.xMax = toggleRect.xMin + syncStretchWidth;
                EditorGUI.BeginChangeCheck();
                tVal = EditorGUI.Toggle(toggleRect, (seq.m_syncType & SyncType.STRETCH) > 0);
                if (EditorGUI.EndChangeCheck()) {
                    if (tVal)
                        seq.m_syncType |= SyncType.STRETCH;
                    else
                        seq.m_syncType &= ~SyncType.STRETCH;
                }
#endregion
#region Tracks
                Rect r = new Rect(toggleRect.xMax + 3f, yPos, objectFieldWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.DrawRect(r, new Color(0.27f, 0.27f, 0.27f, 1f));
                seq.UpdateModifiers();
                float speed = seq.GetSpeedForSync(totalLength);
                Rect trackRect = new Rect(r.x, r.y, 0f, EditorGUIUtility.singleLineHeight);
                int numLoops = 1;
                if ((seq.m_syncType & SyncType.REPEAT) > 0) {
                    float ratio = totalLength / (seq.TotalLength / speed);
                    numLoops = (int)ratio;
                    if (ratio - numLoops > 0)
                        ++numLoops;
                }
                for (int l = 0; l < numLoops; ++l) {
                    for (int c = 0; c < seq.m_clipData.Length; ++c) {
                        if (seq.m_clipData[c].m_clip == null)
                            continue;
                        float clipLength = (seq.m_clipData[c].m_clip.length / speed) / totalLength;
                        trackRect.xMin = trackRect.xMax;
                        trackRect.xMax = Mathf.Min(r.xMax, trackRect.xMin + clipLength * objectFieldWidth);
                        EditorGUI.DrawRect(trackRect, SyncGroupTrackColors[(c + colorOffset) % SyncGroupTrackColors.Length]);
                        EditorGUI.DrawRect(new Rect(trackRect.position, new Vector2(1f, EditorGUIUtility.singleLineHeight)), Color.black);
                        EditorGUI.LabelField(trackRect, seq.m_clipData[c].m_clip.name, EditorStyles.whiteMiniLabel);
                    }
                }
                colorOffset += seq.m_clipData.Length;
#endregion
                r.xMin = r.xMax;
                r.xMax = mainRect.xMax;
                if (GUI.Button(r, removeButtonContent, EditorStyles.miniButtonRight)) {
                    seq.m_syncGroup = "";
                }
                yPos += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
#endregion
        }
        /// <summary> Displays a box to drop Sequence or Modifier assets in to add a new Value </summary>
        void DrawSyncGroupDropArea() {
            Event currentEvent = Event.current;
            GUIContent dropBoxContent = m_editorUtils.GetContent("SyncGroupDropArea");
            float minHeight = Mathf.Max(EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing, EditorSkin.centeredBody.CalcHeight(dropBoxContent, EditorGUIUtility.currentViewWidth));
            GUILayout.Box(GUIContent.none, EditorSkin.box, GUILayout.ExpandWidth(true), GUILayout.MinHeight(minHeight));
            Rect r = GUILayoutUtility.GetLastRect();
            GUIStyle dropBoxStyle = new GUIStyle(EditorSkin.panelLabel);
            dropBoxStyle.alignment = TextAnchor.MiddleCenter;
            dropBoxStyle.wordWrap = true;
            GUI.Label(r, dropBoxContent, dropBoxStyle);
            if (currentEvent.type == EventType.DragUpdated && r.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Sequence && string.IsNullOrEmpty((DragAndDrop.objectReferences[x] as Sequence).m_syncGroup)) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        break;
                    }
                }
            } else if (currentEvent.type == EventType.DragPerform && r.Contains(currentEvent.mousePosition) && DragAndDrop.objectReferences.Length > 0) {
                bool foundOne = false;
                for (int x = 0; x < DragAndDrop.objectReferences.Length; ++x) {
                    if (DragAndDrop.objectReferences[x] is Sequence) {
                        Sequence seq = DragAndDrop.objectReferences[x] as Sequence;
                        if (string.IsNullOrEmpty(seq.m_syncGroup)) {
                            foundOne = true;
                            seq.m_syncGroup = "--InvalidSyncGroupName--";
                        }
                    }
                }
                if (foundOne) {
                    DragAndDrop.AcceptDrag();
                    currentEvent.Use();
                }
            }
        }

        void MonitorTab() {
            AmbienceManager.GetSequences(ref GlobalSequences, ref AreaSequences);
            AllTracks = AmbienceManager.GetTracks();
            GlobalBlocked = AmbienceManager.GetBlocked();
            if (AreaSequences.Count > 0)
                m_editorUtils.Panel("AudioAreaPanel", Runtime_AudioAreaPanel, true);
            if (GlobalSequences.Count > 0)
                m_editorUtils.Panel("AddedSequencesPanel", Runtime_AddedSequencesPanel, true);
            if (AllTracks.Count > 0)
                m_editorUtils.Panel("CurrentlyPlayingPanel", Runtime_CurrentlyPlayingPanel, true);
            if (GlobalBlocked.Count > 0)
                m_editorUtils.Panel("CurrentlyBlockedPanel", Runtime_CurrentlyBlockedPanel, true);
            m_editorUtils.Panel("ValuesPanel", Runtime_ValuesPanel, true);
            m_editorUtils.Panel("EventsPanel", Runtime_EventsPanel, true);
            Repaint();
        }
        /// <summary> Panel to show all currently playing AudioClips </summary>
        /// <param name="inlineHelp">Should help be displayed?</param>
        void Runtime_CurrentlyPlayingPanel(bool inlineHelp) {
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
        void Runtime_CurrentlyBlockedPanel(bool inlineHelp) {
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
        void Runtime_AudioAreaPanel(bool inlineHelp) {
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
        void Runtime_AddedSequencesPanel(bool inlineHelp) {
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
        void Runtime_ValuesPanel(bool inlineHelp) {
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
        void Runtime_EventsPanel(bool inlineHelp) {
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

        /// <summary> Tab to display when something went wrong and we can't access EditorUtils </summary>
        void ErrorTab() {
            GUILayout.Label("Error: Unable to find EditorUtils!", EditorSkin.panelLabel);
        }
#endregion
#region Internal Functions
        /// <summary> Helper function to gather all Value references </summary>
        /// <returns>List of ValueInfo objects for each unique Value name</returns>
        List<AssetInfo> GetAllValues() {
            List<AssetInfo> ret = new List<AssetInfo>();
            foreach (Sequence seq in AllSequences) {
                if ((seq.m_requirements & ValuesOrEvents.Values) == ValuesOrEvents.Values) {
                    for (int x = 0; x < seq.m_values.Length; ++x) {
                        AssetInfo si = ret.Find(delegate (AssetInfo info) {
                            return info.Name == seq.m_values[x].m_name;
                        });
                        if (si == null) {
                            si = new AssetInfo(seq.m_values[x].m_name, seq);
                            ret.Add(si);
                        } else {
                            if (!si.Sequences.Contains(seq))
                                si.Sequences.Add(seq);
                        }
                    }
                }
            }
            foreach (Modifier mod in AllModifiers) {
                if ((mod.m_requirements & ValuesOrEvents.Values) == ValuesOrEvents.Values) {
                    for (int x = 0; x < mod.m_values.Length; ++x) {
                        AssetInfo si = ret.Find(delegate (AssetInfo info) {
                            return info.Name == mod.m_values[x].m_name;
                        });
                        if (si == null) {
                            si = new AssetInfo(mod.m_values[x].m_name, mod);
                            ret.Add(si);
                        } else {
                            if (!si.Modifiers.Contains(mod))
                                si.Modifiers.Add(mod);
                        }
                    }
                }
            }
            return ret;
        }
        /// <summary> Helper function to gather all Value references </summary>
        /// <returns>List of ValueInfo objects for each unique Value name</returns>
        List<AssetInfo> GetAllEvents() {
            List<AssetInfo> ret = new List<AssetInfo>();
            foreach (Sequence seq in AllSequences) {
                if ((seq.m_requirements & ValuesOrEvents.Events) == ValuesOrEvents.Events) {
                    for (int x = 0; x < seq.m_events.Length; ++x) {
                        AssetInfo si = ret.Find(delegate (AssetInfo info) {
                            return info.Name == seq.m_events[x];
                        });
                        if (si == null) {
                            si = new AssetInfo(seq.m_events[x], seq);
                            ret.Add(si);
                        } else {
                            if (!si.Sequences.Contains(seq))
                                si.Sequences.Add(seq);
                        }
                    }
                }
            }
            foreach (Modifier mod in AllModifiers) {
                if ((mod.m_requirements & ValuesOrEvents.Events) == ValuesOrEvents.Events) {
                    for (int x = 0; x < mod.m_events.Length; ++x) {
                        AssetInfo si = ret.Find(delegate (AssetInfo info) {
                            return info.Name == mod.m_events[x];
                        });
                        if (si == null) {
                            si = new AssetInfo(mod.m_events[x], mod);
                            ret.Add(si);
                        } else {
                            if (!si.Modifiers.Contains(mod))
                                si.Modifiers.Add(mod);
                        }
                    }
                }
            }
            return ret;
        }
#endregion
#region ContextMenu Options
        /// <summary> Toggles "Invert" setting on selected Value </summary>
        /// <param name="data">ValueInfo object containing Value and it's parent</param>
        void DoContextMenuInvert(object data) {
            ValueContextInfo info = (ValueContextInfo)data;
            if (info.Value == null || info.parent == null) {
                Debug.LogError("Invalid Info passed to DoContextMenuCopy");
                return;
            }
            info.Value.m_invert = !info.Value.m_invert;
            Repaint();
        }
        /// <summary> Copies data about a Value to the clipboard </summary>
        /// <param name="data">ValueInfo object containing Value and it's parent</param>
        void DoContextMenuCopy(object data) {
            ValueContextInfo info = (ValueContextInfo)data;
            if (info.Value == null || info.parent == null) {
                Debug.LogError("Invalid Info passed to DoContextMenuCopy");
                return;
            }
            EditorGUIUtility.systemCopyBuffer = "Value:"+info.Value.m_min + ";" + info.Value.m_max+";"+info.Value.m_minFalloff+";"+info.Value.m_maxFalloff+";"+(info.Value.m_invert?"T":"F");
        }
        /// <summary> Pastes data from clipboard to a Value </summary>
        /// <param name="data">ValueInfo object containing Value and it's parent</param>
        void DoContextMenuPaste(object data) {
            ValueContextInfo info = (ValueContextInfo)data;
            if (info.Value == null || info.parent == null || EditorGUIUtility.systemCopyBuffer == null || !EditorGUIUtility.systemCopyBuffer.StartsWith("Value:")) {
                Debug.LogError("Invalid Info passed to DoContextMenuPaste");
                return;
            }
            string[] settings = EditorGUIUtility.systemCopyBuffer.Substring(7).Split(new char[] { ';' }, System.StringSplitOptions.None);
            if (settings.Length != 5)
                return;
            float min, max, minFalloff, maxFalloff;
            bool cont = true;
            cont &= float.TryParse(settings[0], out min);
            cont &= float.TryParse(settings[1], out max);
            cont &= float.TryParse(settings[2], out minFalloff);
            cont &= float.TryParse(settings[3], out maxFalloff);
            if (cont) {
                bool invert = settings[4] == "T";
                info.Value.m_min = min;
                info.Value.m_max = max;
                info.Value.m_minFalloff = minFalloff;
                info.Value.m_maxFalloff = maxFalloff;
                info.Value.m_invert = invert;
            }
            Repaint();
        }
        /// <summary> Removes a Value from a Sequence or Modifier </summary>
        /// <param name="data">ValueInfo object containing Value and it's parent</param>
        void DoContextMenuDelete(object data) {
            ValueContextInfo info = (ValueContextInfo)data;
            if (info.Value == null || info.parent == null) {
                Debug.LogError("Invalid Info passed to DoContextMenuDelete");
                return;
            }
            if (info.parent is Modifier) {
                Modifier mod = info.parent as Modifier;
                List<SliderRange> modValues = new List<SliderRange>(mod.m_values);
                modValues.RemoveAll(delegate (SliderRange s) {
                    return s == info.Value;
                });
                mod.m_values = modValues.ToArray();

                if (modValues.Count == 0) //we removed all the Values ... remove the Values requirement
                    mod.m_requirements &= ~ValuesOrEvents.Values;
            } else if (info.parent is Sequence) {
                Sequence seq = info.parent as Sequence;
                List<SliderRange> seqValues = new List<SliderRange>(seq.m_values);
                seqValues.RemoveAll(delegate (SliderRange s) {
                    return s == info.Value;
                });
                seq.m_values = seqValues.ToArray();

                if (seqValues.Count == 0) //we removed all the Values ... remove the Values requirement
                    seq.m_requirements &= ~ValuesOrEvents.Values;
            }
            GUI.FocusControl("");
            Repaint();
        }
#endregion
    }
}