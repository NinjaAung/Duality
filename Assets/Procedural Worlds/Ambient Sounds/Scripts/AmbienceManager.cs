// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * Manager of AmbientSounds system. Does most of the heavy lifting.
 */

namespace AmbientSounds
{
    /// <summary>
    /// Manages inputs and audio selections from various sources and combines them into background audio.
    /// Feeds audio data straight into 'PlaybackSource'.
    /// </summary>
    [AddComponentMenu("Procedural Worlds/Ambient Sounds/Ambience Manager")]
    [DisallowMultipleComponent]
    public class AmbienceManager : MonoBehaviour
    {
        #region INTERNAL USE ONLY - DO NOT TOUCH
        #region Types
        /// <summary> Contains information about an Audio Area and its fade values (used in Update) </summary>
        class SequenceFadeData
        {
            /// <summary> Reference to Audio Areas </summary>
            public AudioArea area = null;
            /// <summary> Fade value to apply </summary>
            public float fade = 0f;
        }
        public struct TrackPlayingInfo
        {
            public string m_name;
            public float m_fadeLevel;
            public float m_volumeLevel;
            public Sequence m_sequence;
        }
        #endregion
        #region Public Variables
        /// <summary> Object to track position of for Audio Areas </summary>
        public Transform m_playerObject = null;
        /// <summary> Global playback speed for all Sequences </summary>
        public float m_playSpeed = 1.0f;
        /// <summary> Global volume applied to all Sequences </summary>
        [Range(0f, 1f)]
        public float m_volume = 1.0f;
        /// <summary> Global Sequences that will always play as long as their requirements are met </summary>
        public Sequence[] m_globalSequences = new Sequence[0];
        /// <summary>
        /// Whether audio clips should be loaded at first chance (or when first played when false).
        /// Sets static variable on Awake()
        /// </summary>
        public bool m_preloadAudio = true;
        /// <summary> Should all audio from this manager be played through an AudioSource? (instead of OnAudioFilter) </summary>
        public bool m_useAudioSource = false;
        /// <summary> Prefab of AudioSource we want to use (if NULL will just create a default one) </summary>
        public GameObject m_audioSourcePrefab = null;
        /// <summary> Number of channels to output when using AudioSource </summary>
        public int m_audioSourceChannels = 2;
        #endregion
        #region Internal Variables
        /// <summary> Internal reference to our created AudioSource </summary>
        AudioSource outputSource = null;
        /// <summary> Internal reference to our created AudioSource's object </summary>
        GameObject outputSourceGO = null;
        /// <summary> last known sample rate (Updated automatically as AudioSettings changes) </summary>
        int lastSampleRate = -1;
        int sourcesCreatedThisFrame = 0;
        #endregion
        #region Unity Lifecycle
        void Awake()
        {
            _instance = this;
            addedSequences.Clear();
            fadeOutToRemoveSequences.Clear();
            lock(trackData)
                trackData.Clear();
            sourceTrackData.Clear();
            immediatePlaySequences.Clear();
            activeEvents.Clear();
            activeValues.Clear();
            loadedAudioClips.Clear();
            loadingAudioClips.Clear();
            s_preloadAudio = m_preloadAudio;
            if (s_preloadAudio)
            {
                foreach (Sequence seq in m_globalSequences)
                {
                    if (seq == null)
                        continue;
                    foreach (Sequence.ClipData clip in seq.m_clipData)
                    {
                        if (clip.m_clip != null)
                            GetAudioData(clip.m_clip);
                    }
                    foreach (Modifier mod in seq.m_modifiers)
                    {
                        if (mod == null || !mod.m_modClips)
                            continue;
                        foreach (Sequence.ClipData clip in mod.m_clipData)
                        {
                            if (clip.m_clip != null)
                                GetAudioData(clip.m_clip);
                        }
                    }
                }
                foreach (AudioArea area in areaSequences)
                    if (area != null)
                        foreach (Sequence seq in area.m_sequences)
                            if (seq != null) {
                                foreach (Sequence.ClipData clip in seq.m_clipData)
                                    if(clip.m_clip != null)
                                        GetAudioData(clip.m_clip);
                                foreach (Modifier mod in seq.m_modifiers)
                                    if (mod != null && mod.m_modClips)
                                        foreach (Sequence.ClipData clip in mod.m_clipData)
                                            if (clip.m_clip != null)
                                                GetAudioData(clip.m_clip);
                            }
            }
            lastSampleRate = AudioSettings.outputSampleRate;
            AudioSettings.OnAudioConfigurationChanged += OnAudioSettingsChanged;
            if (GetComponent<AudioListener>() == null)
            {
                AudioListener al = FindObjectOfType<AudioListener>();
                if (al)
                {
                    Debug.LogWarning("No AudioListener found on this object. Moving to Object '" + al.name + "'.", al);
                    AmbienceManager am = al.gameObject.AddComponent<AmbienceManager>();
                    am.m_playerObject = m_playerObject;
                    am.m_playSpeed = m_playSpeed;
                    am.m_volume = m_volume;
                    am.m_globalSequences = m_globalSequences;
                    am.m_useAudioSource = m_useAudioSource;
                    am.m_audioSourcePrefab = m_audioSourcePrefab;
                    am.m_audioSourceChannels = m_audioSourceChannels;
                    am.m_preloadAudio = m_preloadAudio;
                    Destroy(this);
                    return;
                }
                else
                {
                    Debug.LogWarning("No AudioListener found on this or any active object. Searching for AudioListener...", this);
                    StartCoroutine(CoroFindListener());
                }
            }
            foreach (Sequence seq in m_globalSequences) //update all global sequences' modifier data on load
                if (seq != null)
                    seq.UpdateModifiers();

            if (m_useAudioSource)
                CreateOutputSource();

#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
            EditorApplication.pauseStateChanged += HandleOnPauseStateChanged;
#endif


        }
        /// <summary> Destructor to destroy AudioSource </summary>
        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            if (outputSourceGO)
                Destroy(outputSourceGO);
            else if (outputSource != null)
                Destroy(outputSource);

#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
            EditorApplication.pauseStateChanged -= HandleOnPauseStateChanged;
#endif

        }

#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
        /// <summary>
        /// Check for changes in play mode to pause audio in editor
        /// </summary>
        private void HandleOnPauseStateChanged(PauseState state)
        {
            if (state == PauseState.Paused)
                isUnityPaused = true;
            else
                isUnityPaused = false;
        }
#endif
        ///<summary> check position and update audio track data if needed </summary>
        private void Update()
        {
            if (sourcesCreatedThisFrame > 0)
                sourcesCreatedThisFrame -= 1;
            //this should be the only place we create "AudioTrack"s as they need to be created on the main thread
            if (m_useAudioSource && outputSource == null)
            {
                CreateOutputSource();
            }
            else if (!m_useAudioSource && outputSource != null)
            { //we were using an audio source but don't want to anymore
                if (outputSourceGO)
                    Destroy(outputSourceGO);
                else
                    Destroy(outputSource);
                outputSourceGO = null;
                outputSource = null;
            }
            Vector3 PlayerPos = m_playerObject == null ? (Camera.main == null ? transform.position : Camera.main.transform.position) : m_playerObject.position;
            float now = Time.time;
            Dictionary<Sequence, SequenceFadeData> areaFades = new Dictionary<Sequence, SequenceFadeData>(areaSequences.Count);
            foreach (AudioArea ps in areaSequences)
            {
                float fadeAmt = ps.GetFade(PlayerPos); //fade based on position
                foreach (Sequence data in ps.m_sequences)
                {
                    if (data == null)
                        continue;
                    SequenceFadeData fadeData;
                    if (areaFades.TryGetValue(data, out fadeData))
                    {
                        if (fadeData.fade < fadeAmt)
                        {
                            fadeData.fade = fadeAmt;
                            fadeData.area = ps;
                        }
                    }
                    else
                        areaFades.Add(data, new SequenceFadeData { area = ps, fade = fadeAmt });
                }
            }
            foreach (Sequence data in addedSequences)
                if (data != null)
                {
                    SequenceFadeData fadeData;
                    if (areaFades.TryGetValue(data, out fadeData))
                    {
                        if (fadeData.fade <= 0f)
                            fadeData.area = null;
                        fadeData.fade = 1f;
                    }
                    else
                        areaFades.Add(data, new SequenceFadeData() { fade = 1f });
                }
            foreach (Sequence data in m_globalSequences)
                if (data != null)
                {
                    SequenceFadeData fadeData;
                    if (areaFades.TryGetValue(data, out fadeData))
                    {
                        if (fadeData.fade <= 0f)
                            fadeData.area = null;
                        fadeData.fade = 1f;
                    }
                    else
                        areaFades.Add(data, new SequenceFadeData() { fade = 1f });
                }
            Dictionary<string, float> playingValues = new Dictionary<string, float>();
            foreach (KeyValuePair<Sequence, SequenceFadeData> seq in areaFades) {
                for (int v = 0; v < seq.Key.m_valuesWhilePlaying.Length; ++v) {
                    float val;
                    if (playingValues.TryGetValue(seq.Key.m_valuesWhilePlaying[v], out val)) {
                        if (val < seq.Value.fade)
                            playingValues[seq.Key.m_valuesWhilePlaying[v]] = seq.Value.fade;
                    } else {
                        playingValues.Add(seq.Key.m_valuesWhilePlaying[v], seq.Value.fade);
                    }
                }
                UpdateTrackData(seq.Key, seq.Value.area, seq.Value.fade);
            }
            bool valueChanged = false;
            foreach (KeyValuePair<string, float> v in playingValues) {
                float tmp;
                if (!valueChanged && (!activeValues.TryGetValue(v.Key, out tmp) || tmp != v.Value))
                    valueChanged = true;
                activeValues[v.Key] = v.Value;
            }
            if (valueChanged) {
                foreach (Sequence seq in areaFades.Keys)
                    seq.UpdateModifiers();
            }
            //update all "Play" clips
            for (int s = 0; s < immediatePlaySequences.Count; ++s)
            {
                if (immediatePlaySequences[s] != null)
                {
                    AudioTrack track = UpdateTrackData(immediatePlaySequences[s], null, 1f, true);
                    if (track != null && track.IsFinished(true)) {
                        if (track.StartedPlaying) {
                            try {
                                immediatePlaySequences[s].m_OnPlayClip.Invoke(track.StartedPlaying);
                            } catch (System.Exception e) {
                                Debug.LogError(e);
                            }
                            track.StartedPlaying = null;
                        }
                        if (track.StoppedPlaying) {
                            try {
                                immediatePlaySequences[s].m_OnStopClip.Invoke(track.StoppedPlaying);
                            } catch (System.Exception e) {
                                Debug.LogError(e);
                            }
                            track.StoppedPlaying = null;
                        }
                        immediatePlaySequences.RemoveAt(s--);
                    }
                }
            }
            for (int s = 0; s < fadeOutToRemoveSequences.Count; ++s)
            {
                if (fadeOutToRemoveSequences[s] != null)
                {
                    if (areaFades.ContainsKey(fadeOutToRemoveSequences[s]))
                    {
                        fadeOutToRemoveSequences.RemoveAt(s--);
                        continue;
                    }
                    AudioTrack track = UpdateTrackData(fadeOutToRemoveSequences[s], null, 0f, false);
                    if (track != null && track.IsFinished(true)) {
                        if (track.StartedPlaying) {
                            try {
                                fadeOutToRemoveSequences[s].m_OnPlayClip.Invoke(track.StartedPlaying);
                            } catch (System.Exception e) {
                                Debug.LogError(e);
                            }
                            track.StartedPlaying = null;
                        }
                        if (track.StoppedPlaying) {
                            try {
                                fadeOutToRemoveSequences[s].m_OnStopClip.Invoke(track.StoppedPlaying);
                            } catch (System.Exception e) {
                                Debug.LogError(e);
                            }
                            track.StoppedPlaying = null;
                        }
                        fadeOutToRemoveSequences.RemoveAt(s--);
                    }
                }
            }

            foreach (AudioTrack track in trackData)
                if (track != null && track.m_sequence != null) {
                    if (track.StartedPlaying) {
                        try {
                            track.m_sequence.m_OnPlayClip.Invoke(track.StartedPlaying);
                        } catch (System.Exception e) {
                            Debug.LogError(e);
                        }
                        track.StartedPlaying = null;
                    }
                    if (track.StoppedPlaying) {
                        try {
                            track.m_sequence.m_OnStopClip.Invoke(track.StoppedPlaying);
                        } catch (System.Exception e) {
                            Debug.LogError(e);
                        }
                        track.StoppedPlaying = null;
                    }
                }
            foreach (AudioTrack track in sourceTrackData)
                if (track != null && track.m_sequence != null) {
                    if (track.StartedPlaying) {
                        try {
                            track.m_sequence.m_OnPlayClip.Invoke(track.StartedPlaying);
                        } catch (System.Exception e) {
                            Debug.LogError(e);
                        }
                        track.StartedPlaying = null;
                    }
                    if (track.StoppedPlaying) {
                        try {
                            track.m_sequence.m_OnStopClip.Invoke(track.StoppedPlaying);
                        } catch (System.Exception e) {
                            Debug.LogError(e);
                        }
                        track.StoppedPlaying = null;
                    }
                }
            lock (trackData) {
                trackData.RemoveAll(delegate (AudioTrack track) {
                    bool ret = track == null || track.IsFinished(true);
                    if (ret && track != null && track.m_sequence != null) {
                        foreach (string e in track.m_sequence.m_eventsWhilePlaying)
                            CheckUpdateEvent(e);
                    }
                    return ret;
                });
            }
            sourceTrackData.RemoveAll(delegate (AudioTrack track) {
                bool ret = track == null || track.IsFinished(true);
                if (ret && track != null && track.m_sequence != null) {
                    foreach (string e in track.m_sequence.m_eventsWhilePlaying)
                        CheckUpdateEvent(e);
                }
                return ret;
            });

            //Update RawAudioData that may not be loaded already
            for (int x = 0; x < loadingAudioClips.Count; ++x)
            {
                loadingAudioClips[x].UpdateLoad();
                if (loadingAudioClips[x].IsLoaded)
                {
                    //Debug.Log("Finished loading " + loadingAudioClips[x].Name + " time="+Time.time);
                    loadingAudioClips.RemoveAt(x--);
                }
            }
            //update SyncGroups
            SyncGroup.UpdateAll();
        }
        #endregion
        #region Internal Functions
        /// <summary> Creates AudioSource for main output </summary>
        void CreateOutputSource()
        {
            if (m_audioSourcePrefab)
            {
                GameObject prefab = Instantiate(m_audioSourcePrefab, transform);
                prefab.transform.localPosition = Vector3.zero;
                prefab.transform.localScale = Vector3.one;
                prefab.transform.localRotation = Quaternion.identity;
                outputSourceGO = prefab;
                outputSource = prefab.GetComponent<AudioSource>();
                if (outputSource == null)
                    outputSource = prefab.AddComponent<AudioSource>();
            }
            else
            {
                GameObject prefab = new GameObject("Ambient Sounds Output");
                prefab.transform.parent = transform;
                prefab.transform.localPosition = Vector3.zero;
                prefab.transform.localScale = Vector3.one;
                prefab.transform.localRotation = Quaternion.identity;
                outputSourceGO = prefab;
                outputSource = prefab.AddComponent<AudioSource>();
            }
            outputSource.volume = 1f;
            outputSource.loop = true;
            outputSource.clip = AudioClip.Create("Ambient Sounds Output", lastSampleRate, Mathf.Clamp(m_audioSourceChannels, 1, 8), lastSampleRate, true, OnAudioRead);
            outputSource.Play();
        }
        /// <summary> Updates Sample Rate and AudioClip stream (if used) when AudioSettings change </summary>
        /// <param name="deviceWasChanged">Did a device change caused this to be called?</param>
        void OnAudioSettingsChanged(bool deviceWasChanged)
        {
            lastSampleRate = AudioSettings.outputSampleRate;
            foreach (AudioTrack track in sourceTrackData)
            {
                if (track == null)
                    continue;
                track.m_outputSampleRate = lastSampleRate;
                if (track.m_outputSource != null)
                    track.m_outputSource.clip = AudioClip.Create(track.m_name, 1024, 2, lastSampleRate, true, track.OnAudioRead);
            }
            if (m_useAudioSource)
            {
                if (outputSourceGO)
                    Destroy(outputSourceGO);
                else if (outputSource)
                    Destroy(outputSource);
                CreateOutputSource();
            }
        }
        /// <summary> Coroutine to find a new AudioListener to move script to. </summary>
        IEnumerator CoroFindListener()
        {
            while (true)
            {
                float timer = Time.time + 1f;
                while (Time.time < timer)
                    yield return null;
                AudioListener al = FindObjectOfType<AudioListener>();
                if (al)
                {
                    Debug.Log("Found AudioListener. Moving to Object '" + al.name + "'.", al);
                    AmbienceManager am = al.gameObject.AddComponent<AmbienceManager>();
                    am.m_playerObject = m_playerObject;
                    am.m_playSpeed = m_playSpeed;
                    am.m_globalSequences = m_globalSequences;
                    Destroy(this);
                    yield break;
                }
            }
        }
        /// <summary>
        /// Updates Track Data for passed AmbienceData starting and stopping tracks as needed.
        /// Only run in main thread.
        /// </summary>
        /// <param name="data">Sequence to check</param>
        /// <param name="area">Audio Area this Sequence is being updated from</param>
        /// <param name="fadeAmt">optional Fade value to multiply fade by</param>
        /// <param name="isPlayOnce"></param>
        AudioTrack UpdateTrackData(Sequence data, AudioArea area = null, float fadeAmt = 1f, bool isPlayOnce = false)
        {
            //Debug.Log("UpdateTrackData(" + (data == null ? "NULL" : data.name) + ", " + (area == null ? "NULL" : area.name) + ", " + fadeAmt + ", " + (isPlayOnce ? "True" : "False") + ") called");
            if (data == null)
                return null; //no data? nothing to play then so abort.
            if (!data.m_hasBeenUpdated)
                data.UpdateModifiers();
            if (data.Clips == null || data.Clips.Length == 0)
            {
                foreach (Sequence.ClipData c in data.m_clipData)
                    if (c.m_clip != null && !loadedAudioClips.ContainsKey(c.m_clip))
                        GetAudioData(c.m_clip); //trying to play but not loaded yet ... start the load
                return null;//no clips? nothing to play then so abort.
            }
            float valueFade = data.m_forcePlay ? 1f : (fadeAmt * data.FadeValue);
            bool shouldPlay = !s_disabled && valueFade > 0f;
            AudioTrack track;
            if ((area != null && area.m_outputType != OutputType.STRAIGHT) || data.m_outputDirect || data.m_outputType != OutputType.STRAIGHT) {
                track = null;
                for (int t = 0; t < sourceTrackData.Count; ++t) {
                    if (sourceTrackData[t].IsPlayOnce == isPlayOnce && sourceTrackData[t].m_sequence == data) {
                        track = sourceTrackData[t];
                        break;
                    }
                }
                if (track == null) { //see if it is in the other list? (possible if a Sequence is in Global list and a AudioArea)
                    for (int t = 0; t < trackData.Count; ++t) {
                        if (trackData[t].IsPlayOnce == isPlayOnce && trackData[t].m_sequence == data) {
                            track = trackData[t];
                            break;
                        }
                    }
                    if (track != null)
                    { //we found one so we need to move it to the right list
                        lock(trackData)
                            trackData.Remove(track);
                        CreateAudioSource(track, data, area);
                        sourceTrackData.Add(track);
                    }
                }
            }
            else
            {
                track = null;
                for (int t = 0; t < trackData.Count; ++t) {
                    if (trackData[t].IsPlayOnce == isPlayOnce && trackData[t].m_sequence == data) {
                        track = trackData[t];
                        break;
                    }
                }
                if (track == null) { //see if it is in the other list? (possible if a Sequence is in Global list and a AudioArea)
                    for (int t = 0; t < sourceTrackData.Count; ++t) {
                        if (sourceTrackData[t].IsPlayOnce == isPlayOnce && sourceTrackData[t].m_sequence == data) {
                            track = sourceTrackData[t];
                            break;
                        }
                    }
                    if (track != null)
                    { //we found one so we need to move it to the right list
                        sourceTrackData.Remove(track);
                        lock (trackData)
                            trackData.Add(track);
                        if (track.m_outputSource != null)
                        {
                            Destroy(track.m_outputSource.gameObject);
                            track.m_outputSource = null;
                        }
                    }
                }
            }
            if (track != null && track.m_isOutputDirect)
            {
                if (track.m_outputSource == null)
                    CreateAudioSource(track, data, area);
                track.UpdateDirect();
            }
            if (!shouldPlay)
            {
                if (track != null && track.m_fadeTarget > 0f)
                {
                    //Debug.Log("Stopping Track " + data.name);
                    track.m_fadeTarget = 0f;
                }
                return track; //not playing and shouldn't play ... our job is done here
            }
            else
            {
                if (track != null)
                { //already playing .. just update fade and move if needed
                    if (track.m_isOutputDirect)
                    {
                        if (track.m_outputNeedsIntialized)
                        {
                            CreateAudioSource(track, data, area);
                            track.m_outputNeedsMoved = false;
                            track.m_outputNeedsMovedDelay = 0f;
                            track.m_outputNeedsIntialized = false;
                        }
                        else if (track.m_outputNeedsMoved)
                        {
                            track.m_outputNeedsMoved = false;
                            MoveAudioSource(track, data, area);
                            track.m_outputNeedsMovedDelay = 0f;
                        }
                        else
                            UpdateAudioSource(track, data, area);
                    }
                    else if (track.m_outputNeedsMoved)
                    {
                        track.m_outputNeedsMoved = false;
                        MoveAudioSource(track, data, area);
                        track.m_outputNeedsMovedDelay = 0f;
                    }
                    else
                        UpdateAudioSource(track, data, area);
                    track.m_fadeTarget = valueFade;
                }
                else
                { //start playing
                    data.UpdateModifiers(); //update modifier-based info
                    if ((area != null && area.m_outputType != OutputType.STRAIGHT) || data.m_outputType != OutputType.STRAIGHT)
                    {
                        AudioTrack newTrack = new AudioTrack(data, isPlayOnce) { m_fadeTarget = valueFade };
                        CreateAudioSource(newTrack, data, area);
                        sourceTrackData.Add(newTrack);
                        //Debug.Log("Created AudioSource for track " + newTrack.m_name, newTrack.m_outputSource);
                    }
                    else
                    {
                        //Debug.Log("Creating Track for " + data.name);
                        lock(trackData)
                            trackData.Add(new AudioTrack(data, isPlayOnce) { m_fadeTarget = valueFade });
                    }
                    foreach (string e in data.m_eventsWhilePlaying)
                        CheckUpdateEvent(e);
                }
            }
            return track;
        }
        /// <summary> Creates an AudioSource for a Track to play through </summary>
        /// <param name="track">Track to play through source</param>
        /// <param name="sequence">Sequence that Track is based off of</param>
        /// <param name="area">Audio Area that contained Sequence (if any)</param>
        GameObject CreateAudioSource(AudioTrack track, Sequence sequence, AudioArea area)
        {
            GameObject sourceGO, prefab;
            Transform parent;
            if (area == null || area.m_outputType == OutputType.STRAIGHT)
            {
                if (!sequence.m_outputFollowPosition || !sequence.m_outputFollowRotation)
                    parent = null;
                else
                    parent = m_playerObject ?? transform;
                prefab = sequence.m_outputPrefab;
            }
            else
            {
                if (!sequence.m_outputFollowPosition || !sequence.m_outputFollowRotation)
                    parent = null;
                else
                    parent = area.m_outputType == OutputType.LOCAL_POSITION ? area.transform : m_playerObject ?? transform;
                prefab = area.m_outputPrefab ?? sequence.m_outputPrefab;
            }

            if (track.m_outputSource != null)
            {
                sourceGO = track.m_outputSource.gameObject;
            }
            else
            {
                if (prefab != null) {
                    sourceGO = Instantiate(prefab, parent);
                    sourceGO.name = "Source_" + track.m_name;
                } else {
                    sourceGO = new GameObject("Source_" + track.m_name);
                    sourceGO.transform.SetParent(parent);
                }
            }
            track.curFollowing = parent;
            sourceGO.transform.localScale = Vector3.one;
            if (parent == null)
                sourceGO.transform.position = transform.position;
            else
                sourceGO.transform.localPosition = Vector3.zero;
            sourceGO.transform.localRotation = Quaternion.identity;

            track.m_myManager = this;
            track.m_outputSampleRate = lastSampleRate;
            if (track.m_outputSource == null)
                if ((track.m_outputSource = sourceGO.GetComponent<AudioSource>()) == null)
                {
                    track.m_outputSource = sourceGO.AddComponent<AudioSource>();
                    track.m_outputSource.spatialBlend = 1f;
                    track.m_outputSource.spatialize = true;
                }
            if (track.m_isOutputDirect || sequence.m_outputDirect)
            {
                track.m_isOutputDirect = true;
                track.m_outputSource.loop = false;
            }
            else
                StartCoroutine(CoroCreateSource(track, sourcesCreatedThisFrame++));
            MoveAudioSource(track, sequence, area);
            return sourceGO;
        }
        IEnumerator CoroCreateSource(AudioTrack track, int delay) {
            int targetFrame = Time.frameCount + delay;
            while (Time.frameCount < targetFrame)
                yield return null;
            if (track == null || track.m_outputSource == null)
                yield break;
            track.m_outputSource.loop = true;
            track.m_outputSource.clip = AudioClip.Create(track.m_name, lastSampleRate, 2, lastSampleRate, true, track.OnAudioRead);
            track.m_outputSource.Play();
        }
        /// <summary> Updates rotation and distance values for a Track based on AudioArea or Sequence settings </summary>
        /// <param name="track">Track AudioSource belongs to</param>
        /// <param name="sequence">Sequence that Track is based off of</param>
        /// <param name="area">Audio Area that contained Sequence (if any)</param>
        void MoveAudioSource(AudioTrack track, Sequence sequence, AudioArea area)
        {
            if (track.m_outputSource == null)
                return;
            if (area == null || area.m_outputType == OutputType.STRAIGHT)
            {
                track.curFollowing = m_playerObject ?? transform;
                if (sequence == null)
                {
                    track.curVerticalRotation = 0f;
                    track.curHorizontalRotation = 0f;
                    track.curDistance = 0f;
                    track.curFollowPosition = true;
                    track.curFollowRotation = true;
                }
                else
                {
                    track.curVerticalRotation = -Helpers.GetRandom(sequence.m_outputVerticalAngle);
                    track.curHorizontalRotation = -Helpers.GetRandom(sequence.m_outputHorizontalAngle);
                    track.curDistance = Helpers.GetRandom(sequence.m_outputDistance);
                    track.curFollowPosition = sequence.m_outputFollowPosition;
                    track.curFollowRotation = sequence.m_outputFollowRotation;
                }
            }
            else
            {
                track.curFollowing = area.m_outputType == OutputType.LOCAL_POSITION ? area.transform : m_playerObject ?? transform;
                track.curVerticalRotation = -Helpers.GetRandom(area.m_outputVerticalAngle);
                track.curHorizontalRotation = -Helpers.GetRandom(area.m_outputHorizontalAngle);
                track.curDistance = Helpers.GetRandom(area.m_outputDistance);
                track.curFollowPosition = area.m_outputFollowPosition;
                track.curFollowRotation = area.m_outputFollowRotation;
            }

            if (track.m_outputNeedsMovedDelay > 0)
            {
                StartCoroutine(CoroMoveAudioSource(track, sequence, area));
            }
            else
            {
                if (!track.curFollowPosition && !track.curFollowRotation)
                {
                    Transform follow;
                    if (area == null || area.m_outputType == OutputType.STRAIGHT)
                    {
                        if (!sequence.m_outputFollowPosition || !sequence.m_outputFollowRotation)
                            follow = transform;
                        else
                            follow = m_playerObject;
                    }
                    else
                    {
                        if (!sequence.m_outputFollowPosition || !sequence.m_outputFollowRotation)
                            follow = transform;
                        else
                            follow = area.m_outputType == OutputType.LOCAL_POSITION ? area.transform : m_playerObject;
                    }
                    track.m_outputSource.transform.position = (follow ?? transform).position + Quaternion.Euler(track.curVerticalRotation, track.curHorizontalRotation, 0f) * Vector3.forward * track.curDistance;
                }
                else
                    UpdateAudioSource(track, sequence, area);
            }
        }
        IEnumerator CoroMoveAudioSource(AudioTrack track, Sequence sequence, AudioArea area)
        {
            if (track == null)
                yield break;
            float timer = Time.time + track.m_outputNeedsMovedDelay;
            while (Time.time < timer)
                yield return null;
            if (track == null || track.m_outputSource == null)
                yield break;
            if (!track.curFollowPosition && !track.curFollowRotation)
            {
                Transform follow;
                if (area == null || area.m_outputType == OutputType.STRAIGHT)
                {
                    if (!sequence.m_outputFollowPosition || !sequence.m_outputFollowRotation)
                        follow = transform;
                    else
                        follow = m_playerObject;
                }
                else
                {
                    if (!sequence.m_outputFollowPosition || !sequence.m_outputFollowRotation)
                        follow = transform;
                    else
                        follow = area.m_outputType == OutputType.LOCAL_POSITION ? area.transform : m_playerObject;
                }
                track.m_outputSource.transform.position = (follow ?? transform).position + Quaternion.Euler(track.curVerticalRotation, track.curHorizontalRotation, 0f) * Vector3.forward * track.curDistance;
            }
            else
                UpdateAudioSource(track, sequence, area);
        }
        /// <summary> Updates position of AudioSource for a Track </summary>
        /// <param name="track">Track AudioSource belongs to</param>
        /// <param name="sequence">Sequence that Track is based off of</param>
        /// <param name="area">Audio Area that contained Sequence (if any)</param>
        void UpdateAudioSource(AudioTrack track, Sequence sequence, AudioArea area)
        {
            if (track.m_outputSource == null)
                return;
            if (track.curFollowPosition && track.curFollowRotation)
            { //should be attached so no update needed unless it's not
                if (track.m_outputSource.transform.parent != track.curFollowing) //needs to be re-attached
                    track.m_outputSource.transform.SetParent(track.curFollowing);
                track.m_outputSource.transform.localPosition = Quaternion.Euler(track.curVerticalRotation, track.curHorizontalRotation, 0f) * Vector3.forward * track.curDistance;
            }
            else if (track.curFollowPosition)
            { //should be detatched and updated to follow position
                if (track.m_outputSource.transform.parent != null)
                    track.m_outputSource.transform.SetParent(null);
                if (track.curFollowing == null)
                    return;
                track.m_outputSource.transform.localPosition = track.curFollowing.position + Quaternion.Euler(track.curVerticalRotation, track.curHorizontalRotation, 0f) * Vector3.forward * track.curDistance;
            }
        }
        #endregion
        #region Audio Filter Read
        /// <summary> Called by AudioSource to retrieve the next set of audio graph data </summary>
        /// <param name="data">Array to be filled with Audio graph data</param>
        private void OnAudioRead(float[] data)
        {
            if (isUnityPaused || !m_useAudioSource)
                return;
            for (int x = 0; x < data.Length; ++x)
                data[x] = 0f;
            OnAudioReadInternal(data, m_audioSourceChannels);
        }
        /// <summary> Called by AudioListener or AudioSource to allow script to alter audio graph. </summary>
        /// <param name="data">Array of floats containing current audio graph data.</param>
        /// <param name="channels">Number of audio channels in data.</param>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (isUnityPaused || m_useAudioSource)
                return;
            OnAudioReadInternal(data, channels);
        }
        /// <summary> Internal function to get a set of frames called by either OnAudioRead or OnAudioFilterRead </summary>
        /// <param name="data">Array to be filled with Audio graph data</param>
        /// <param name="channels">Number of channels expected to be output</param>
        private void OnAudioReadInternal(float[] data, int channels)
        {
            double timeStep = (1.0 / lastSampleRate) * m_playSpeed;
            float FadeStep = 1f / lastSampleRate;
            lock (trackData) {
                for (int t = 0; t < trackData.Count; ++t)
                    if (trackData[t] != null)
                        trackData[t].OnBeforeAudioRead();
                for (int p = 0; p < data.Length / channels; ++p) {
                    for (int t = 0; t < trackData.Count; ++t) {
                        AudioTrack Track = trackData[t];
                        if (Track == null)
                            continue;
                        Track.UpdateFade(FadeStep);
                        if (Track.IsFinished())
                            continue;
                        Track.GetFrame(timeStep, m_volume, data, p * channels, channels);
                    }
                }
            }
        }
        #endregion
        #region Static Variables
        static AmbienceManager _instance = null;
        /// <summary> Whether audio clips should be loaded at first chance (or when first played when false) </summary>
        public static bool s_preloadAudio = true;

        /// <summary> Event called every time a Value value has been changed or a Value is removed </summary>
        public static event System.Action<string> OnValueChanged;
        /// <summary> Event called every time an "Event" is added or removed </summary>
        public static event System.Action<string> OnEventChanged;

        /// <summary> Is entire system disabled? </summary>
        static bool s_disabled = false;
        /// <summary> Is Unity Paused? </summary>
        internal static bool isUnityPaused = false;
        /// <summary> The List of tracks currently playing through AudioListener. </summary>
        static List<AudioTrack> trackData = new List<AudioTrack>();
        /// <summary> The List of tracks currently playing through AudioSources. </summary>
        static List<AudioTrack> sourceTrackData = new List<AudioTrack>();
        /// <summary> List of all loading audio clips and their RawAudioData (reduces number of clips checked every Update() to only ones still loading) </summary>
        static List<RawAudioData> loadingAudioClips = new List<RawAudioData>();
        /// <summary> List of all loaded (and loading) audio clips and their RawAudioData </summary>
        static Dictionary<AudioClip, RawAudioData> loadedAudioClips = new Dictionary<AudioClip, RawAudioData>();
        /// <summary> List of AudioAreas to play while PlayerObject is in area </summary>
        static List<AudioArea> areaSequences = new List<AudioArea>();
        /// <summary> List of Sequences added at runtime through AddSequence </summary>
        static List<Sequence> addedSequences = new List<Sequence>();
        /// <summary> List of 'One-Shot' Sequences to play once (one clip) and discard </summary>
        static List<Sequence> fadeOutToRemoveSequences = new List<Sequence>();
        /// <summary> List of 'One-Shot' Sequences to play once (one clip) and discard </summary>
        static List<Sequence> immediatePlaySequences = new List<Sequence>();
        /// <summary> List of active Events for Sequence requirements </summary>
        static List<string> activeEvents = new List<string>();
        /// <summary> List of active Values and their values for Sequence requirements </summary>
        static Dictionary<string, float> activeValues = new Dictionary<string, float>();
        #endregion
        #region Static Interfaces
        #region AudioData
        /// <summary> Gets cached RawAudioData for a given AudioClip </summary>
        /// <param name="clip">AudioClip to get data for</param>
        /// <returns>RawAudioData for given clip</returns>
        internal static RawAudioData GetAudioData(AudioClip clip)
        {
            if (loadedAudioClips.ContainsKey(clip))
                return loadedAudioClips[clip];
            RawAudioData ret = new RawAudioData(clip);
            loadedAudioClips.Add(clip, ret);
            if (!ret.IsLoaded)
            {
                //Debug.Log("Started loading " + ret.Name + " time=" + Time.time);
                loadingAudioClips.Add(ret);
            }
            return ret;
        }
        #endregion

        #region Track Update Functions
        /// <summary> Updates any Track if it uses an Event named 'EventName' </summary>
        /// <param name="EventName">Event that changed</param>
        internal static void CheckUpdateEvent(string EventName) {
            foreach (AudioTrack track in trackData)
                CheckUpdateEvent(track, EventName);
            foreach (AudioTrack track in sourceTrackData)
                CheckUpdateEvent(track, EventName);
        }
        /// <summary> Updates a Track if it uses an Event named 'EventName' </summary>
        /// <param name="track">Track to update</param>
        /// <param name="EventName">Event that changed</param>
        internal static void CheckUpdateEvent(AudioTrack track, string EventName)
        {
            if (track == null || track.m_sequence == null)
                return;
            bool needsUpdate = false;
            foreach (Modifier mod in track.m_sequence.m_modifiers)
            {
                if (mod == null)
                    continue;
                foreach (string e in mod.m_events)
                {
                    if (e == EventName)
                    {
                        needsUpdate = true;
                        break;
                    }
                }
                if (needsUpdate)
                    break;
            }
            if (needsUpdate)
            {
                track.m_sequence.UpdateModifiers();
                track.UpdateClips();
            }
        }
        /// <summary> Updates a Track if it uses a Value named 'valueName' </summary>
        /// <param name="track">Track to update</param>
        /// <param name="valueName">Name of value that was changed</param>
        internal static void CheckUpdateValue(AudioTrack track, string valueName)
        {
            if (track == null || track.m_sequence == null)
                return;
            bool needsUpdate = false;
            foreach (Modifier mod in track.m_sequence.m_modifiers)
            {
                if (mod == null)
                    continue;
                foreach (SliderRange s in mod.m_values)
                {
                    if (s.m_name == valueName)
                    {
                        needsUpdate = true;
                        break;
                    }
                }
                if (needsUpdate)
                    break;
            }
            if (needsUpdate)
            {
                track.m_sequence.UpdateModifiers();
                track.UpdateClips();
            }
        }
        #endregion

        #region Add/Remove audio sets
        /// <summary> Adds a AudioArea object with this Manager </summary>
        /// <param name="area">AudioArea to register (usually 'this')</param>
        internal static void AddArea(AudioArea area)
        {
            if (area == null)
                return;
            if (s_preloadAudio)
            {
                foreach (Sequence seq in area.m_sequences)
                    if (seq != null)
                    {
                        foreach (Sequence.ClipData clip in seq.m_clipData)
                            if (clip.m_clip != null)
                                GetAudioData(clip.m_clip);
                        foreach (Modifier mod in seq.m_modifiers)
                            if (mod != null && mod.m_modClips)
                                foreach (Sequence.ClipData clip in mod.m_clipData)
                                    if (clip.m_clip != null)
                                        GetAudioData(clip.m_clip);
                        seq.UpdateModifiers();
                    }
            }
            if (!areaSequences.Contains(area))
                areaSequences.Add(area);
            foreach (Sequence seq in area.m_sequences)
                if (seq != null)
                    fadeOutToRemoveSequences.Remove(seq);
        }
        /// <summary> Removes a AudioArea object from this Manager </summary>
        /// <param name="area">AudioArea to deregister (usually 'this')</param>
        internal static void RemoveArea(AudioArea area)
        {
            if (area == null)
                return;
            areaSequences.Remove(area);
            foreach (Sequence seq in area.m_sequences)
                if (seq != null)
                    fadeOutToRemoveSequences.Add(seq);
        }
        #endregion

        #region Editor events
        /// <summary> Called by Editor if a sequence is removed from m_globalSequences list while playing </summary>
        /// <param name="seq">Sequence that is being removed</param>
        public static void OnEditorRemovedSequence(Sequence seq)
        {
            if (!fadeOutToRemoveSequences.Contains(seq))
                fadeOutToRemoveSequences.Add(seq);
        }
        #endregion

        #region Check Value/Event functions
        /// <summary> Will check if either Events list is empty or if we have an active Event that matches one in the list. </summary>
        /// <param name="events">Array of Events to check for</param>
        /// <param name="eventsMix">EvaluationType to use</param>
        /// <returns>True if Events is empty or if activeEvents contains one of the Events</returns>
        internal static bool CheckEvents(string[] events, EvaluationType eventsMix)
        {
            if (events == null || events.Length == 0)
                return true;
            if (eventsMix == EvaluationType.NONE)
            {
                foreach (string str in events)
                    if (!string.IsNullOrEmpty(str))
                        if (GetEventActive(str))
                            return false;
                return true;
            }
            else if (eventsMix == EvaluationType.ALL)
            {
                foreach (string str in events)
                    if (!string.IsNullOrEmpty(str))
                        if (!GetEventActive(str))
                            return false;
                return true;
            }
            else
            { //if (eventsMix == EvaluationType.ANY) {
                bool validEventsFound = false;
                foreach (string str in events)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        validEventsFound = true;
                        if (GetEventActive(str))
                            return true;
                    }
                }
                return !validEventsFound;
            }
        }
        /// <summary> Gets fade of audio data based on it's Value Mix and Value Falloff </summary>
        /// <param name="values">Array of Values to check</param>
        /// <param name="valuesMix">EvaluationType to use</param>
        /// <param name="addIfMissing">Should Values be added automatically if missing?</param>
        /// <returns>Float between 0 and 1 for percent of volume this audio data to play at</returns>
        internal static float CheckValues(SliderRange[] values, EvaluationType valuesMix, bool addIfMissing = true)
        {
            if (values == null || values.Length == 0)
                return 1f;
            if (valuesMix == EvaluationType.ALL)
            {
                float ret = 1f;
                foreach (SliderRange value in values)
                {
                    if (value == null || string.IsNullOrEmpty(value.m_name))
                        continue;
                    float fade = value.Eval(GetValue(value.m_name, addIfMissing));
                    ret = Mathf.Min(ret, fade);
                }
                return ret;
            }
            else
            {
                float ret = 0f;
                bool foundValidValue = false;
                foreach (SliderRange value in values)
                {
                    if (value == null || string.IsNullOrEmpty(value.m_name))
                        continue;
                    foundValidValue = true;
                    float fade = value.Eval(GetValue(value.m_name, addIfMissing));
                    ret = Mathf.Max(ret, fade);
                }
                if (!foundValidValue)
                    return 1f;
                return valuesMix == EvaluationType.NONE ? 1f - ret : ret;
            }
        }
        #endregion

        #region Info Functions
        /// <summary> Fills Lists with all active Sequence data </summary>
        /// <param name="Globals"></param>
        /// <param name="Globals"></param>
        /// <param name="Areas"></param>
        public static void GetSequences(ref List<Sequence> Globals, ref List<AudioArea> Areas)
        {
            if (Globals != null) {
                Globals.Clear();
                Globals.AddRange(addedSequences);
            }
            if (Areas != null) {
                Areas.Clear();
                Areas.AddRange(areaSequences);
            }
        }
        /// <summary> Gets a List of all playing tracks and their fade values </summary>
        /// <returns>List containing all playing tracks and their fade values</returns>
        public static List<TrackPlayingInfo> GetTracks()
        {
            List<TrackPlayingInfo> ret = new List<TrackPlayingInfo>();
            foreach (AudioTrack track in trackData)
            {
                RawAudioData curPlaying = track.CurPlaying;
                ret.Add(new TrackPlayingInfo()
                {
                    m_name = curPlaying == null ? "NULL" : curPlaying.Name,
                    m_fadeLevel = track.CurFade,
                    m_volumeLevel = track.CurVolume,
                    m_sequence = track.m_sequence
                });
            }
            foreach (AudioTrack track in sourceTrackData)
            {
                RawAudioData curPlaying = track.CurPlaying;
                ret.Add(new TrackPlayingInfo()
                {
                    m_name = curPlaying == null ? "NULL" : curPlaying.Name,
                    m_fadeLevel = track.CurFade,
                    m_volumeLevel = track.CurVolume,
                    m_sequence = track.m_sequence
                });
            }
            return ret;
        }
        /// <summary> Gets a List of all Audio Tracks that are not playing due to Requirements </summary>
        /// <returns>List of Sequences and reason strings</returns>
        public static Dictionary<Sequence, string> GetBlocked() {
            Dictionary<Sequence, string> ret = new Dictionary<Sequence, string>();
            if (_instance != null) {
                foreach (Sequence seq in _instance.m_globalSequences)
                    if (seq != null && seq.FadeValue <= 0f && !ret.ContainsKey(seq))
                        ret.Add(seq, GetBlockedReason(seq));
            }
            foreach (Sequence seq in addedSequences)
                if (seq.FadeValue <= 0f && !ret.ContainsKey(seq))
                    ret.Add(seq, GetBlockedReason(seq));
            foreach (AudioArea area in areaSequences)
                foreach(Sequence seq in area.m_sequences)
                    if (seq.FadeValue <= 0f && !ret.ContainsKey(seq))
                        ret.Add(seq, GetBlockedReason(seq));
            return ret;
        }
        /// <summary> Gets string reason for why this Sequence is not playing. (Does not check if it is playing) </summary>
        /// <param name="seq">Sequence to get reason for</param>
        /// <returns>Reason this Sequence is not playing</returns>
        static string GetBlockedReason(Sequence seq) {
            if (seq == null)
                return "NULL";
            string reason = "";
            bool addedVal = false;
            if ((seq.m_requirements & ValuesOrEvents.Values) == ValuesOrEvents.Values) {
                foreach (SliderRange val in seq.m_values) {
                    float v = GetValue(val.m_name, false);
                    bool invert = val.m_invert;
                    if (seq.m_valuesMix == EvaluationType.NONE)
                        invert = !invert;
                    if (invert) {
                        if (v >= val.m_min - val.m_minFalloff && v <= val.m_max + val.m_maxFalloff) {
                            if (addedVal) {
                                reason += ", " + val.m_name;
                            } else {
                                addedVal = true;
                                reason += "Value: " + val.m_name;
                            }
                        }
                    } else {
                        if (v <= val.m_min - val.m_minFalloff || v >= val.m_max + val.m_maxFalloff) {
                            if (addedVal) {
                                reason += ", " + val.m_name;
                            } else {
                                addedVal = true;
                                reason += "Value: " + val.m_name;
                            }
                        }
                    }
                }
            }
            if ((seq.m_requirements & ValuesOrEvents.Events) == ValuesOrEvents.Events) {
                bool addedEvent = false;
                foreach (string e in seq.m_events) {
                    bool isActive = GetEventActive(e);
                    if ((seq.m_eventsMix == EvaluationType.NONE && isActive) || (seq.m_eventsMix != EvaluationType.NONE && !isActive)) {
                        if (addedEvent) {
                            reason += ", " + e;
                        } else {
                            if (addedVal)
                                reason += " & ";
                            reason += "Event: " + e;
                            addedEvent = true;
                        }
                    }
                }
            }
            return reason;
        }
        /// <summary> Sets Values and/or Events required to make Sequence play </summary>
        /// <param name="seq">Sequence to set Values and/or Events based on</param>
        public static void Debug_UnBlockSequence(Sequence seq) {
            if (seq == null)
                return;
            if ((seq.m_requirements & ValuesOrEvents.Values) == ValuesOrEvents.Values) {
                foreach (SliderRange val in seq.m_values) {
                    bool invert = val.m_invert;
                    if (seq.m_valuesMix == EvaluationType.NONE)
                        invert = !invert;
                    if (invert) {
                        if (val.m_min - val.m_minFalloff > 0f) {
                            SetValue(val.m_name, 0f);
                        } else if (val.m_max + val.m_maxFalloff < 1f) {
                            SetValue(val.m_name, 1f);
                        } else {
                            Debug.LogError("Unable to set Value "+val.m_name+" for "+seq.name+"! Any value will disable it.");
                        }
                    } else {
                        SetValue(val.m_name, val.m_min + (val.m_max - val.m_min) * 0.5f);
                    }
                }
            }
            if ((seq.m_requirements & ValuesOrEvents.Events) == ValuesOrEvents.Events) {
                if (seq.m_eventsMix == EvaluationType.NONE) {
                    foreach (string e in seq.m_events)
                        DeactivateEvent(e);
                } else {
                    foreach (string e in seq.m_events)
                        ActivateEvent(e);
                }
            }
        }
        #endregion
        #endregion
        #endregion

        #region AMBIENT SOUNDS API - USE THESE TO INTEGRATE AMBIENT SOUNDS WITH YOUR GAME
        #region Event functions
        /// <summary> Activates an Event. Will influence Sequences and Modifiers that use this Event in their Requirements.</summary>
        /// <param name="EventName">Event to activate</param>
        public static void ActivateEvent(string EventName)
        {
            if (!activeEvents.Contains(EventName))
            {
                activeEvents.Add(EventName);
                foreach (AudioTrack track in trackData)
                    CheckUpdateEvent(track, EventName);
                foreach (AudioTrack track in sourceTrackData)
                    CheckUpdateEvent(track, EventName);
                if (OnEventChanged != null)
                    OnEventChanged.Invoke(EventName);
            }
        }
        /// <summary> Deactivates an Event. Will influence Sequences and Modifiers that use this Event in their Requirements.</summary>
        /// <param name="EventName">Event to deactivate</param>
        public static void DeactivateEvent(string EventName)
        {
            if (activeEvents.Contains(EventName))
            {
                activeEvents.Remove(EventName);
                foreach (AudioTrack track in trackData)
                    CheckUpdateEvent(track, EventName);
                foreach (AudioTrack track in sourceTrackData)
                    CheckUpdateEvent(track, EventName);
                if (OnEventChanged != null)
                    OnEventChanged.Invoke(EventName);
            }
        }
        /// <summary> Checks if a single Event is active </summary>
        /// <param name="e">Event to check for</param>
        /// <returns>True if e is found in active events or sequence events</returns>
        public static bool GetEventActive(string EventName)
        {
            if (string.IsNullOrEmpty(EventName))
                return false;

            if (activeEvents.Contains(EventName))
                return true;
            foreach (AudioTrack track in trackData)
                if (track.m_sequence != null && !track.IsFinished())
                    foreach (string E in track.m_sequence.m_eventsWhilePlaying)
                        if (E == EventName)
                            return true;
            foreach (AudioTrack track in sourceTrackData)
                if (track.m_sequence != null && !track.IsFinished())
                    foreach (string E in track.m_sequence.m_eventsWhilePlaying)
                        if (E == EventName)
                            return true;
            return false;
        }
        /// <summary> Gets a list of all active Ambient Sounds Events.</summary>
        /// <returns>String Array active Event names</returns>
        public static string[] GetEvents()
        {
            List<string> allEvents = new List<string>(activeEvents);
            foreach (AudioTrack track in trackData)
                if (track.m_sequence != null)
                    foreach (string e in track.m_sequence.m_eventsWhilePlaying)
                        if (!allEvents.Contains(e))
                            allEvents.Add(e);
            foreach (AudioTrack track in sourceTrackData)
                if (track.m_sequence != null)
                    foreach (string e in track.m_sequence.m_eventsWhilePlaying)
                        if (!allEvents.Contains(e))
                            allEvents.Add(e);
            return allEvents.ToArray();
        }
        #endregion

        #region Value functions
        /// <summary> Gets a listing of all active Value checks and their current values.</summary>
        /// <returns>Dictionary containing a copy of Value names and their values</returns>
        public static Dictionary<string, float> GetValues()
        {
            return new Dictionary<string, float>(activeValues);
        }
        /// <summary> Sets a value for a Value Check in the Manager </summary>
        /// <param name="Name">Name of the value to set</param>
        /// <param name="Value">Value to set the value to (0.0 - 1.0)</param>
        public static void SetValue(string Name, float Value)
        {
            if (activeValues.ContainsKey(Name))
                activeValues[Name] = Mathf.Clamp01(Value);
            else
                activeValues.Add(Name, Mathf.Clamp01(Value));
            foreach (AudioTrack track in trackData)
                CheckUpdateValue(track, Name);
            foreach (AudioTrack track in sourceTrackData)
                CheckUpdateValue(track, Name);
            if (OnValueChanged != null)
                OnValueChanged.Invoke(Name);
        }
        /// <summary> Gets the current Value for a Value Check from the Manager </summary>
        /// <param name="Name">Name of value to get</param>
        /// <param name="AddIfMissing">Should this Value be added if missing?</param>
        /// <returns>Current value of specified Value check</returns>
        public static float GetValue(string Name, bool AddIfMissing = true)
        {
            if (activeValues.ContainsKey(Name))
                return activeValues[Name];
            if (AddIfMissing)
                activeValues.Add(Name, 0f);
            return 0f;
        }
        /// <summary> Removes the current value of a Value Check from the Manager </summary>
        /// <param name="Name">Name of value to remove</param>
        public static void RemoveValue(string Name)
        {
            if (activeValues.ContainsKey(Name))
            {
                activeValues.Remove(Name);
                foreach (AudioTrack track in trackData)
                    CheckUpdateValue(track, Name);
                foreach (AudioTrack track in sourceTrackData)
                    CheckUpdateValue(track, Name);
                if (OnValueChanged != null)
                    OnValueChanged.Invoke(Name);
            }
        }
        #endregion

        #region Global Sequence control
        /// <summary> Adds a Sequence to the "Global Sequences" list for immediate playback.</summary>
        /// <param name="seq">Sequence to add</param>
        public static void AddSequence(Sequence seq)
        {
            if (seq == null || addedSequences.Contains(seq))
                return;
            foreach (Sequence.ClipData clip in seq.m_clipData)
                if (clip.m_clip != null)
                    GetAudioData(clip.m_clip);
            foreach (Modifier mod in seq.m_modifiers)
                if (mod != null && mod.m_modClips)
                    foreach (Sequence.ClipData clip in mod.m_clipData)
                        if (clip.m_clip != null)
                            GetAudioData(clip.m_clip);
            fadeOutToRemoveSequences.Remove(seq);
            addedSequences.Add(seq);
            seq.UpdateModifiers();
        }
        /// <summary> Removes a Sequence from the "Global Sequences" list </summary>
        /// <param name="seq">Sequence to remove</param>
        public static void RemoveSequence(Sequence seq)
        {
            if (seq == null)
                return;
            if (addedSequences.Remove(seq) && !fadeOutToRemoveSequences.Contains(seq))
            {
                seq.m_forcePlay = false;
                fadeOutToRemoveSequences.Add(seq);
            }
        }
        /// <summary> Gets whether Sequence seq was added by AddSequence() and can be removed with RemoveSequence() </summary>
        /// <param name="seq">Sequence to search for</param>
        /// <returns>True if Sequence seq has been loaded and can be played</returns>
        public static bool WasSequenceAdded(Sequence seq)
        {
            return addedSequences.Contains(seq);
        }
        /// <summary> Gets whether Sequence seq is currently active and will be played if it's conditions are met </summary>
        /// <param name="seq">Sequence to search for</param>
        /// <returns>True if Sequence seq has been loaded and can be played</returns>
        public static bool IsSequenceActive(Sequence seq)
        {
            if (addedSequences.Contains(seq))
                return true;
            foreach (AudioArea aa in areaSequences)
            {
                foreach (Sequence s in aa.m_sequences)
                    if (s == seq)
                        return true;
            }
            return false;
        }
        #endregion

        #region Playback control
        /// <summary> Preloads an AudioClip for use in Play() later. </summary>
        /// <param name="clip">AudioClip to load</param>
        public static void PreloadAudioClip(AudioClip clip)
        {
            if (clip != null)
                GetAudioData(clip);
        }
        /// <summary> Preloads a Sequence for use in AddSequence later. </summary>
        /// <param name="seq">Sequence to preload</param>
        public static void PreloadSequence(Sequence seq)
        {
            foreach (Sequence.ClipData clip in seq.m_clipData)
                if (clip.m_clip != null)
                    GetAudioData(clip.m_clip);
            foreach (Modifier mod in seq.m_modifiers)
                if (mod != null && mod.m_modClips)
                    foreach (Sequence.ClipData clip in mod.m_clipData)
                        if (clip.m_clip != null)
                            GetAudioData(clip.m_clip);
        }
        /// <summary> Plays an AudioClip as soon as possible once. </summary>
        /// <param name="clip">Clip to play</param>
        /// <param name="volume">Volume level to play at</param>
        public static void Play(AudioClip clip, float volume)
        {
            if (clip == null)
                return;
            if (s_preloadAudio) //a little late to request but better late than never i guess (should use PreloadAudioClip(clip) before trying to push a new clip)
                GetAudioData(clip);
            Sequence seq = ScriptableObject.CreateInstance<Sequence>();
            seq.m_clipData = new Sequence.ClipData[] { new Sequence.ClipData(clip, 1f) };
            seq.m_volume = Mathf.Clamp01(volume);
            seq.UpdateModifiers();
            immediatePlaySequences.Add(seq);
        }
        /// <summary> Gets whether an AudioClip is currently being played through Play() function </summary>
        /// <param name="clip">AudioClip to search for</param>
        /// <returns>True if AudioClip is currently playing</returns>
        public static bool IsPlaying(AudioClip clip)
        {
            for (int s = 0; s < immediatePlaySequences.Count; ++s) {
                foreach (Sequence.ClipData c in immediatePlaySequences[s].Clips) {
                    if (c.m_clip == clip)
                        return true;
                }
            }
            return false;
        }
        /// <summary> Disables AmbientSound playback. </summary>
        public static void Disable()
        {
            s_disabled = true;
        }
        /// <summary> Enables AmbientSound playback. </summary>
        public static void Enable()
        {
            s_disabled = false;
        }
        /// <summary> Pauses AmbientSound playback. </summary>
        public static void PausePlayback()
        {
            isUnityPaused = true;
        }
        /// <summary> Continues AmbientSound playback. </summary>
        public static void ContinuePlayback()
        {
            isUnityPaused = false;
        }
        #endregion
        #endregion
    }
}
