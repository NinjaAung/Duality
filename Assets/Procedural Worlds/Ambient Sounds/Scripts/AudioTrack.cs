// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;

/*
 * Contains information about specific audio clips and thier raw data
 */

namespace AmbientSounds {

    /// <summary>
    /// Contains Information about a single audio clip parsed for easy access. (most AudioClip data can only be accessed in main thread)
    /// </summary>
    internal class RawAudioData {
        #region Public Properties
        /// <summary> raw audio data broken into [channel][frame] </summary>
        public float[][] RawData {
            get; private set;
        }
        /// <summary> name of audio clip </summary>
        public string Name {
            get; private set;
        }
        /// <summary> sample rate of audio clip </summary>
        public int SampleRate {
            get; private set;
        }
        /// <summary> number of channels in audio clip </summary>
        public int ChannelCount {
            get; private set;
        }
        /// <summary> length of audio clip in seconds </summary>
        public float Length {
            get; private set;
        }
        /// <summary> Whether this clip has been loaded yet </summary>
        public bool IsLoaded {
            get; private set;
        }
        /// <summary> AudioClip this data is based on </summary>
        public AudioClip BaseClip {
            get; private set;
        }
        #endregion
        #region Constructor
        public RawAudioData(AudioClip Clip) {
            IsLoaded = false;
            BaseClip = Clip;
            SampleRate = 0;
            ChannelCount = 0;
            Length = 0f;
            RawData = null;
            if (BaseClip == null) {
                Name = "NULL";
                return;
            }
            Name = Clip.name;
            //read in all data now so we can reference it as needed. (can only read from main thread)
            if (BaseClip.loadState != AudioDataLoadState.Loaded) {
                if (BaseClip.loadState == AudioDataLoadState.Unloaded && BaseClip.loadInBackground) { //not loaded yet and set to load in background. we need to start the load then wait.
                    BaseClip.LoadAudioData();
                    return;
                }
                if (BaseClip.loadState == AudioDataLoadState.Loading) //not loaded yet but already started. we need to just wait
                    return;
                if (!BaseClip.LoadAudioData()) { //
                    Debug.LogError("Unable to load audio data for clip '" + BaseClip.name + "'!");
                    Length = 0f;
                    return;
                }
            }
            ReadAudioData();
        }
        /// <summary> Reads in AudioClip data if the clip has been loaded </summary>
        #endregion
        #region Public Functions
        /// <summary> Attempts to load data if not already loaded. (Call only from main thread) </summary>
        public void UpdateLoad() {
            if (!IsLoaded) {
                ReadAudioData();
            }
        }
        /// <summary> Gets a single frame of audio from this clip </summary>
        /// <param name="time">Time in seconds since start of clip to get</param>
        /// <param name="volume">Volume level to output</param>
        /// <param name="ret">Array to fill with frame data</param>
        /// <param name="startOffset">Offset of ret to begin writing at</param>
        /// <param name="numChannels">Number of channels expected</param>
        public void GetFrame(double time, float volume, float[] ret, int startOffset, int numChannels) {
            if (IsLoaded) {
                double frame = time * SampleRate;
                for (int chan = 0; chan < numChannels; ++chan)
                    ret[startOffset + chan] += Helpers.CubicInterp(RawData[chan % ChannelCount], frame) * volume;
            }
        }
        #endregion
        #region Internal Functions
        /// <summary> Attempts to read audio data from clip. (Call only from main thread) </summary>
        void ReadAudioData() {
            if (BaseClip == null || BaseClip.loadState != AudioDataLoadState.Loaded)
                return;
            if (BaseClip.loadType != AudioClipLoadType.DecompressOnLoad) {
                IsLoaded = true; //can never load a streaming or compressed clip
                return;
            }
            Name = BaseClip.name;
            SampleRate = BaseClip.frequency;
            Length = BaseClip.length;
            int clipFrequency = BaseClip.frequency;
            ChannelCount = BaseClip.channels;
            float[] realData = new float[BaseClip.samples * ChannelCount];
            if (!BaseClip.GetData(realData, 0)) {
                Debug.LogError("Unable to get audio graph data for clip '" + BaseClip.name + "'!");
                Length = 0f;
                return;
            }

            int frameCount = realData.Length / ChannelCount;
            RawData = new float[ChannelCount][];
            for (int chan = 0; chan < ChannelCount; ++chan) {
                RawData[chan] = new float[frameCount];
                for (int frame = 0; frame < frameCount; ++frame) {
                    RawData[chan][frame] = realData[frame * ChannelCount + chan];
                }
            }
            IsLoaded = true;
        }
        #endregion
    }
    /// <summary>
    /// Contains information about a playing "Audio Track". Should only ever be created in main thread.
    /// </summary>
    internal class AudioTrack {
        #region Public Variables
        /// <summary> target fade level to fade towards (if less than or equal to 0, track will be removed when CurFade reaches 0) </summary>
        public float m_fadeTarget = 1f;
        /// <summary> reference to AudioData that this track is based on </summary>
        readonly public Sequence m_sequence;
        /// <summary> name of this track (either base AudioData's name or Clip's name) </summary>
        readonly public string m_name;
        /// <summary> AudioSource this Track is playing through (if it is not set to outputType "Straight") </summary>
        public AudioSource m_outputSource = null;
        /// <summary> Is this Track set to play as OutputDirect? </summary>
        public bool m_isOutputDirect = false;
        /// <summary> Does this track's output need to be re-intialized? (usually from a streaming clip trying to play without outputDirect) </summary>
        public bool m_outputNeedsIntialized = false;
        /// <summary> Sample rate of AudioSource clip </summary>
        public int m_outputSampleRate = 0;
        /// <summary> Does this Track's output position need to be updated? </summary>
        public bool m_outputNeedsMoved = false;
        public float m_outputNeedsMovedDelay = 0f;
        /// <summary> Reference to AmbienceManager for global play speed and volume </summary>
        public AmbienceManager m_myManager = null;
        #endregion
        #region Public Properties
        /// <summary> track's current fade in/out </summary>
        public float CurFade {
            get; private set;
        }
        /// <summary> track's current Volume taking fade and volume modifiers into account </summary>
        public float CurVolume {
            get {
                return Mathf.Clamp01(CurFade * curVolume * curVolumeMod);
            }
        }
        public float CurPlaybackSpeed {
            get {
                return Mathf.Max(0.01f, curPlaybackSpeed * curPlaybackSpeedMod);
            }
        }
        /// <summary> Gets currently playing clip data </summary>
        public RawAudioData CurPlaying {
            get {
                if (PlayToFade != null)
                    return PlayToFade;
                else if (currentClipIdx >= 0 && currentClipIdx < RawClipData.Length) {
                    return RawClipData[currentClipIdx];
                } else
                    return null;
            }
        }
        /// <summary> Was this AudioTrack added by "Play" function? </summary>
        public bool IsPlayOnce {
            get; private set;
        }
        /// <summary> Gets length of currently playing track (Only call from main thread as it will error if isOutputDirect is true otherwise) </summary>
        public float Length {
            get {
                if (m_isOutputDirect) {
                    if (m_outputSource && m_outputSource.clip != null)
                        return m_outputSource.clip.length;
                } else {
                    RawAudioData curTrack = CurPlaying;
                    if (curTrack != null)
                        return curTrack.Length;
                }
                return 0f;
            }
        }
        #endregion
        #region Internal Variables
        /// <summary> tracks current playback speed </summary>
        private float curPlaybackSpeed = 1f;
        /// <summary> tracks current randomized volume modification </summary>
        private float curPlaybackSpeedMod = 1f;
        /// <summary> tracks current volume </summary>
        private float curVolume = 1f;
        /// <summary> tracks current randomized volume modification </summary>
        private float curVolumeMod = 1f;
        /// <summary> currently playing clip index </summary>
        private int currentClipIdx = -1;
        /// <summary> internal value for next clip to play index (use NextClipIdx) </summary>
        private int _nextClipIdx = -1;
        ///<summary> helper to automatically select the next track to play the first time called (set to -1 to reset for next clip) </summary>
        private int NextClipIdx {
            get {
                if (_nextClipIdx < 0) {
                    int numClips;
                    if (m_isOutputDirect)
                        numClips = (DirectClipData == null) ? 0 : DirectClipData.Length;
                    else
                        numClips = (RawClipData == null) ? 0 : RawClipData.Length;
                    if (numClips > 0) {
                        if (++trackQueuePos >= numClips || trackQueue == null) { //we hit the end so we need to reset queue
                            //set up Random order always just in case it gets changed in editor
                            List<int> playlist = new List<int>(numClips);
                            for (int i = 0; i < numClips; ++i)
                                playlist.Insert(Helpers.GetRandom(0, i), i);
                            trackQueuePos = 0;
                            trackQueue = playlist.ToArray();
                        }
                        _nextClipIdx = (m_sequence != null && m_sequence.m_randomizeOrder) ? trackQueue[trackQueuePos] : trackQueuePos;
                    }
                    else
                        _nextClipIdx = 0;
                }
                return _nextClipIdx;
            }
            set {
                _nextClipIdx = value;
            }
        }
        /// <summary> Array containing all relevant clip data </summary>
        private RawAudioData[] RawClipData;
        /// <summary> Array containing AudioClips for OutputDirect play. </summary>
        private AudioClip[] DirectClipData;
        /// <summary> Contains last playing clip to finish fade-out when clips list changes </summary>
        private RawAudioData PlayToFade = null;
        /// <summary> Did Clips list change while in OutputDirect mode? </summary>
        private bool DirectFadeOut = false;
        /// <summary> Time in seconds since this track started playing </summary>
        private double trackTime = 0;
        /// <summary> last trackTime value before this wave of GetFrame() calls </summary>
        private double lastTrackTime = 0;
        /// <summary> True if waiting for Clip's TrackDelayTime before playing next track </summary>
        private bool isDelaying = false;
        /// <summary> Number of audio channels to output if isOutputDirect is true </summary>
        const int outputChannelCount = 2;

        /// <summary> Is this Track controlled by a SyncGroup </summary>
        bool isSynced = false;
        /// <summary> Current queue position for this loop </summary>
        int trackQueuePos = 0;
        /// <summary> Queue of tracks if using random order </summary>
        int[] trackQueue = null;
        /// <summary> Internal reference to SyncGroup for this Track (Use MySyncGroup) </summary>
        SyncGroup _mySyncGroup = null;
        /// <summary> Reference to SyncGroup for this Track (NULL if none) </summary>
        internal SyncGroup MySyncGroup {
            get {
                if (m_sequence == null || string.IsNullOrEmpty(m_sequence.m_syncGroup))
                    return null;
                if (_mySyncGroup == null) {
                    _mySyncGroup = SyncGroup.Get(m_sequence.m_syncGroup);
                    _mySyncGroup.Add(this);
                }
                return _mySyncGroup;
            }
        }
        /// <summary> Used by AmbienceManager to store rotation data </summary>
        internal float curHorizontalRotation = 0f;
        /// <summary> Used by AmbienceManager to store rotation data </summary>
        internal float curVerticalRotation = 0f;
        /// <summary> Used by AmbienceManager to store distance data </summary>
        internal float curDistance = 0f;
        /// <summary> Used by AmbienceManager to store if position should be followed </summary>
        internal bool curFollowPosition = true;
        /// <summary> Used by AmbienceManager to store if rotation should be followed </summary>
        internal bool curFollowRotation = false;
        /// <summary> Used by AmbienceManager to store what Transform should be followed </summary>
        internal Transform curFollowing = null;
        internal AudioClip StartedPlaying = null;
        internal AudioClip StoppedPlaying = null;
        bool crossFadeFlag = false;
        float GlobalVolume {
            get {
                return m_myManager != null ? m_myManager.m_volume : 1f;
            }
        }
        #endregion
        #region Constructor
        /// <summary>
        /// Constructor using AmbienceData as the source
        /// </summary>
        /// <param name="source">AmbienceData to base this track on</param>
        /// <param name="startingTime">Time in seconds from start of track to begin playback</param>
        public AudioTrack(Sequence source, bool playOnce = false) {
            IsPlayOnce = playOnce;
            CurFade = playOnce ? 1f : 0f;
            trackTime = 0.0;
            m_sequence = source;
            if (m_sequence == null) {
                Debug.LogError("Cannot create AudioTrack data for null source.");
                RawClipData = new RawAudioData[0];
                DirectClipData = new AudioClip[0];
                m_name = "NULL";
                return;
            }
            curPlaybackSpeed = m_sequence.PlaybackSpeed;
            curVolume = m_sequence.Volume;
            m_name = m_sequence.name;
            //check for nulls and skip them
            List<AudioClip> allClips = new List<AudioClip>(m_sequence.Clips.Length);
            foreach (Sequence.ClipData clip in m_sequence.Clips) {
                if (clip.m_clip != null) {
                    if (clip.m_clip.loadType != AudioClipLoadType.DecompressOnLoad) {
                        m_outputNeedsIntialized = true;
                        m_isOutputDirect = true; //Cannot read streaming or compressed clip data
                    }
                    allClips.Add(clip.m_clip);
                }
            }
            if (allClips.Count == 0)
                Debug.LogWarning("No audio clips found in Sequence '" + m_name + "'!");
            
            //get the actual audio data for the clips
            DirectClipData = allClips.ToArray();
            RawClipData = new RawAudioData[allClips.Count];
            for (int c = 0; c < RawClipData.Length; ++c)
                RawClipData[c] = AmbienceManager.GetAudioData(allClips[c]);
            currentClipIdx = NextClipIdx;
            if (m_sequence.m_clipData.Length > currentClipIdx)
                curVolume *= m_sequence.m_clipData[currentClipIdx].m_volume;
            if (m_sequence.RandomizeVolume)
                curVolumeMod = Helpers.GetRandom(m_sequence.MinMaxVolume);
            else
                curVolumeMod = 1f;
            if (!isSynced && m_sequence.RandomizePlaybackSpeed)
                curPlaybackSpeedMod = Helpers.GetRandom(m_sequence.MinMaxPlaybackSpeed);
            else
                curPlaybackSpeedMod = 1f;
            if (MySyncGroup == null && m_sequence.TrackDelayTime > 0) {
                //Debug.Log("Starting AudioTrack with delay of " + source.TrackDelayTime);
                isDelaying = true;
            }
        }
        /// <summary>
        /// Constructor using a single AudioClip as a source with optional AmbienceData source it came from
        /// </summary>
        /// <param name="clip">AudioClip to base AudioTrack on</param>
        /// <param name="source">AmbienceData with settings for audio clip</param>
        /// <param name="startingTime">Time in seconds from start of track to begin playback</param>
        public AudioTrack(AudioClip clip, Sequence source, bool playOnce = false) {
            IsPlayOnce = playOnce;
            CurFade = playOnce ? 1f : 0f;
            trackTime = 0.0;
            m_sequence = source;
            if (clip == null) {
                Debug.LogError("Cannot create AudioTrack data for null source clip.");
                RawClipData = new RawAudioData[0];
                DirectClipData = new AudioClip[0];
                return;
            }
            if (clip.loadType != AudioClipLoadType.DecompressOnLoad) {
                m_outputNeedsIntialized = true;
                m_isOutputDirect = true; //Cannot read streaming or compressed clip data
            }
            m_name = clip.name;
            DirectClipData = new AudioClip[1] { clip };
            RawClipData = new RawAudioData[1] { AmbienceManager.GetAudioData(clip) };
            currentClipIdx = 0;
            if (m_sequence != null) {
                curPlaybackSpeed = m_sequence.PlaybackSpeed;
                curVolume = m_sequence.Volume;
                for (int i = 0; i < m_sequence.Clips.Length; ++i) {
                    if (m_sequence.Clips[i].m_clip == clip) {
                        curVolume *= m_sequence.Clips[i].m_volume;
                        break;
                    }
                }
                if (MySyncGroup == null && m_sequence.TrackDelayTime > 0) {
                    //Debug.Log("Starting AudioTrack with delay of " + source.TrackDelayTime);
                    isDelaying = true;
                }
                if (m_sequence.RandomizeVolume)
                    curVolumeMod = Helpers.GetRandom(m_sequence.MinMaxVolume);
                else
                    curVolumeMod = 1f;
                if (!isSynced && m_sequence.RandomizePlaybackSpeed)
                    curPlaybackSpeedMod = Helpers.GetRandom(m_sequence.MinMaxPlaybackSpeed);
                else
                    curPlaybackSpeedMod = 1f;
            } else {
                curPlaybackSpeed = 1f;
                curPlaybackSpeedMod = 1f;
                curVolume = 1f;
                curVolumeMod = 1f;
            }
        }

        /// <summary> Destructor - remove syncGroup reference </summary>
        internal void OnDestroy() {
            if (m_sequence != null) {
                if (!string.IsNullOrEmpty(m_sequence.m_syncGroup)) {
                    SyncGroup sg = SyncGroup.Get(m_sequence.m_syncGroup);
                    if (sg != null)
                        sg.Remove(this);
                }
            }
        }
        #endregion
        #region Public Functions
        public void OnBeforeAudioRead() {
            lastTrackTime = trackTime;
        }
        /// <summary> updates this Track's direct play clips (plays raw audioClip on AudioSource bypassing processing) </summary>
        public void UpdateDirect() {
            if (m_outputSource == null)
                return;
            float timeStep = Time.deltaTime;
            UpdateFade(timeStep);
            if (m_sequence != null) {
                m_outputSource.mute = m_sequence.m_forceMuted;
            }

            m_outputSource.pitch = (CurPlaybackSpeed * m_myManager.m_playSpeed);
            if (m_outputSource.clip == null) {
                //not been set up yet so set it all up.
                trackTime = 0.0;
                currentClipIdx = NextClipIdx;
                NextClipIdx = -1;
                m_outputSource.clip = DirectClipData[currentClipIdx];
                m_outputSource.loop = false;
                m_outputSource.volume = CurVolume * GlobalVolume;
                isDelaying = false;
                DirectFadeOut = false;
                //Debug.Log("Initial Setup done."); 
                return;
            } else { //only add to track time if this isn't the first time we are calling this.
                m_outputSource.volume = CurVolume * GlobalVolume;
                trackTime += timeStep;
            }
            if (IsPlayOnce && !m_outputSource.isPlaying) {
                isDelaying = true;
                //Debug.Log("PlayOnce finished.");
                return;
            }
            if (DirectFadeOut) { //Clips list changed and we need to fade the current clip then change it.
                CurFade = m_sequence != null && m_sequence.m_trackFadeTime > 0 ? Mathf.MoveTowards(CurFade, 0f, timeStep / m_sequence.m_trackFadeTime) : 0f;
                if (CurFade <= 0) {
                    StoppedPlaying = m_outputSource.clip;
                    CurFade = m_sequence != null && m_sequence.m_trackFadeTime > 0 ? Mathf.MoveTowards(CurFade, m_fadeTarget, timeStep / m_sequence.m_trackFadeTime) : m_fadeTarget;
                    currentClipIdx = NextClipIdx;
                    NextClipIdx = -1;
                    m_outputSource.clip = m_sequence.Clips[currentClipIdx].m_clip;
                    m_outputSource.Play();
                    StartedPlaying = m_sequence.Clips[currentClipIdx].m_clip;
                    //Debug.Log("FadeOut ended. Starting clip " + m_outputSource.clip.name);
                    trackTime = 0.0;
                    m_outputSource.volume = CurVolume * GlobalVolume;
                    DirectFadeOut = false;
                } else {
                    //Debug.Log("Waiting for FadeOut");
                    m_outputSource.volume = CurVolume * GlobalVolume;
                    return;
                }
            } else {
                CurFade = m_sequence != null && m_sequence.m_trackFadeTime > 0 ? Mathf.MoveTowards(CurFade, m_fadeTarget, timeStep / m_sequence.m_trackFadeTime) : m_fadeTarget;
                m_outputSource.volume = CurVolume * GlobalVolume;
            }
            if (m_sequence != null && isDelaying) {
                if (trackTime >= m_sequence.TrackDelayTime) {
                    trackTime = 0.0;
                    m_outputSource.Play();
                    //Debug.Log("Ending delay of " + m_clipData.TrackDelayTime + " seconds.");
                    isDelaying = false;
                    m_sequence.TrackDelayTime = -1;
                } else
                    return;
            }
            if (!m_outputSource.isPlaying) {
                if (m_sequence != null && m_sequence.RandomizeVolume)
                    curVolumeMod = Helpers.GetRandom(m_sequence.MinMaxVolume);
                else
                    curVolumeMod = 1f;
                if (!isSynced && m_sequence.RandomizePlaybackSpeed)
                    curPlaybackSpeedMod = Helpers.GetRandom(m_sequence.MinMaxPlaybackSpeed);
                else
                    curPlaybackSpeedMod = 1f;
                StoppedPlaying = m_outputSource.clip;
                if (!isSynced && m_sequence.TrackDelayTime > 0f) {
                    isDelaying = true;
                    m_outputNeedsMovedDelay = (float)(trackTime - lastTrackTime) + m_sequence.TrackDelayTime;
                    m_outputNeedsMoved = true;
                    //Debug.Log("Starting delay of " + m_clipData.TrackDelayTime + " seconds.");
                    trackTime = 0.0;
                    currentClipIdx = NextClipIdx;
                    NextClipIdx = -1;
                    m_outputSource.clip = m_sequence.Clips[currentClipIdx].m_clip;
                } else {
                    currentClipIdx = NextClipIdx;
                    NextClipIdx = -1;
                    m_outputSource.clip = m_sequence.Clips[currentClipIdx].m_clip;
                    //Debug.Log("Starting clip " + m_outputSource.clip.name);
                    m_outputSource.Play();
                    trackTime = 0.0;
                    m_sequence.TrackDelayTime = -1;
                }
                StartedPlaying = m_sequence.Clips[currentClipIdx].m_clip;
            }
        }
        /// <summary> Called by AudioSource while playing created clip. Fills data array with audio graph data. </summary>
        /// <param name="data">Float array to fill with audio graph</param>
        public void OnAudioRead(float[] data) {
            for (int frame = 0; frame < data.Length; ++frame)
                data[frame] = 0f;
            if (AmbienceManager.isUnityPaused) {
                return;
            }
            float FadeStep = 1f / m_outputSampleRate;
            double timeStep = FadeStep * m_myManager.m_playSpeed;
            OnBeforeAudioRead();
            int numFrames = data.Length / outputChannelCount;
            for (int frame = 0; frame < numFrames; ++frame) {
                UpdateFade(FadeStep);
                GetFrame(timeStep, m_myManager.m_volume, data, frame*outputChannelCount, outputChannelCount);
            }
        }
        /// <summary>
        /// Gets a single audio frame with passed number of audio channels
        /// </summary>
        /// <param name="step">Time in seconds to add to track time</param>
        /// <param name="volume">Volume level to multiply audio data by</param>
        /// <param name="ret">Array to fill with audio data</param>
        /// <param name="startOffset">Starting offset of Array to begin writing data</param>
        /// <param name="numChannels">Number of audio channels to create (will duplicate data if numChannels exceeds channel count in clip)</param>
        public void GetFrame(double step, float volume, float[] ret, int startOffset, int numChannels) {
            if (m_isOutputDirect || RawClipData.Length == 0)
                return;
            //advance time for this frame
            trackTime += step * CurPlaybackSpeed;
            //ensure we are within the bounds of the clips
            if (currentClipIdx < 0)
                currentClipIdx = 0;
            else if (currentClipIdx >= RawClipData.Length)
                currentClipIdx = RawClipData.Length - 1;
            RawAudioData currentClipData = PlayToFade ?? RawClipData[currentClipIdx];
            if (!currentClipData.IsLoaded) //wait for it to finish loading
                return;
            if (IsPlayOnce && trackTime >= currentClipData.Length) {
                isDelaying = true;
                return;
            }
            if (PlayToFade != null) {
                if (m_sequence != null && m_sequence.m_trackFadeTime > 0)
                    CurFade = Mathf.MoveTowards(CurFade, 0f, (float)step / m_sequence.m_trackFadeTime);
                else
                    CurFade = 0f;
                if (CurFade > 0f) {
                    if (m_sequence == null || !m_sequence.m_forceMuted)
                        PlayToFade.GetFrame(trackTime, CurVolume * volume, ret, startOffset, numChannels);
                    return;
                } else {
                    StoppedPlaying = PlayToFade.BaseClip;
                    //Debug.Log("PlayToFade ended.");
                    PlayToFade = null;
                    if (IsPlayOnce)
                        isDelaying = true;
                    trackTime = 0.0;
                }
            }
            if (m_sequence != null && isDelaying) {
                if (trackTime >= m_sequence.TrackDelayTime) {
                    trackTime -= m_sequence.TrackDelayTime;
                    //Debug.Log("Ending delay of " + m_clipData.TrackDelayTime + " seconds.");
                    isDelaying = false;
                    lock(m_sequence.ClipsLockHandle)
                        StartedPlaying = m_sequence.Clips[currentClipIdx].m_clip;
                    m_sequence.TrackDelayTime = -1;
                } else {
                    return;
                }
            }
            //check for end of track and skip to next if needed
            while (trackTime >= currentClipData.Length) {
                lock(m_sequence.ClipsLockHandle)
                    StoppedPlaying = m_sequence.Clips[currentClipIdx].m_clip;
                if (m_sequence != null) {
                    if (!isSynced && m_sequence.TrackDelayTime > 0f) {
                        isDelaying = true;
                        m_outputNeedsMovedDelay = (float)(trackTime - lastTrackTime) + m_sequence.TrackDelayTime;
                        m_outputNeedsMoved = true;
                        //Debug.Log("Starting delay of " + m_clipData.TrackDelayTime + " seconds.");
                        trackTime -= currentClipData.Length;
                        currentClipIdx = NextClipIdx;
                        if (m_sequence.RandomizeVolume)
                            curVolumeMod = Helpers.GetRandom(m_sequence.MinMaxVolume);
                        else
                            curVolumeMod = 1f;
                        if (!isSynced && m_sequence.RandomizePlaybackSpeed)
                            curPlaybackSpeedMod = Helpers.GetRandom(m_sequence.MinMaxPlaybackSpeed);
                        else
                            curPlaybackSpeedMod = 1f;
                        //reset our property values to get a new value for next check
                        NextClipIdx = -1;
                        crossFadeFlag = false;
                    } else {
                        if (isSynced && NextClipIdx < 0) //can't repeat so make sure we don't try to go past the end
                            return;
                        m_outputNeedsMovedDelay = (float)(trackTime - lastTrackTime);
                        m_outputNeedsMoved = true;
                        float crossFadeValue = Mathf.Min(m_sequence.m_crossFade, Mathf.Min(currentClipData.Length / 2.0f, RawClipData[NextClipIdx].Length / 2.0f));
                        trackTime -= currentClipData.Length - crossFadeValue;
                        currentClipIdx = NextClipIdx;
                        if (m_sequence.RandomizeVolume)
                            curVolumeMod = Helpers.GetRandom(m_sequence.MinMaxVolume);
                        else
                            curVolumeMod = 1f;
                        if (!isSynced && m_sequence.RandomizePlaybackSpeed)
                            curPlaybackSpeedMod = Helpers.GetRandom(m_sequence.MinMaxPlaybackSpeed);
                        else
                            curPlaybackSpeedMod = 1f;
                        //reset our property values to get a new value for next check
                        NextClipIdx = -1;
                        m_sequence.TrackDelayTime = -1f;
                        if (!crossFadeFlag)
                            lock(m_sequence.ClipsLockHandle)
                                StartedPlaying = m_sequence.Clips[currentClipIdx].m_clip;
                        else
                            crossFadeFlag = false;
                    }
                } else { //end of track but we don't have any base data
                    return;
                }
            }
            //see if we need to do a cross-fade
            if (m_sequence != null && m_sequence.m_forceMuted)
                return;
            if (!IsPlayOnce && ((isSynced && NextClipIdx >= 0) || m_sequence.TrackDelayTime <= 0f) && m_sequence != null && m_sequence.m_crossFade > 0f) {
                float crossFadeValue = Mathf.Min(m_sequence.m_crossFade, Mathf.Min(currentClipData.Length / 2.0f, RawClipData[NextClipIdx].Length / 2.0f)); //don't want cross-fade to last longer than half of either track.
                float timeLeft = currentClipData.Length - (float)trackTime;
                if (timeLeft <= crossFadeValue) {
                    if (!crossFadeFlag) {
                        lock(m_sequence.ClipsLockHandle)
                            StartedPlaying = m_sequence.Clips[NextClipIdx].m_clip;
                        crossFadeFlag = true;
                    }
                    float crossFadeTime = crossFadeValue - timeLeft;
                    float crossFadeAmount = (float)Helpers.Interpolate(crossFadeTime, 0.0, 1.0, crossFadeValue, true, true);
                    currentClipData.GetFrame(trackTime, CurVolume * (1f - crossFadeAmount) * volume, ret, startOffset, numChannels);
                    RawClipData[NextClipIdx].GetFrame(crossFadeTime, CurVolume * crossFadeAmount * volume, ret, startOffset, numChannels);
                    return;
                } else
                    crossFadeFlag = false;
            }
            //generate the data for this frame
            float Fade = 1f;
            if (!isSynced && !IsPlayOnce && m_sequence.TrackDelayTime > 0 && m_sequence != null && m_sequence.m_delayFadeTime > 0f) { //no crossfade but we need to fade out to delay
                float timeLeft = currentClipData.Length - (float)trackTime;
                if (timeLeft <= m_sequence.m_delayFadeTime)
                    Fade = (timeLeft - m_sequence.m_delayFadeTime) / m_sequence.m_delayFadeTime;
            }
            currentClipData.GetFrame(trackTime, CurVolume * Fade * volume, ret, startOffset, numChannels);
        }
        /// <summary>
        /// Gets whether this AudioTrack is finished and ready to be destroyed
        /// </summary>
        /// <returns>True if this track can no longer play</returns>
        public bool IsFinished(bool destroy = false) {
            bool ret = (RawClipData == null || RawClipData.Length == 0)
                || (m_fadeTarget <= 0f && CurFade <= 0f)
                || (IsPlayOnce && isDelaying);
            if (ret && destroy) {
                if (m_outputSource != null) {
                    //Debug.Log("IsFinished() removing AudioSource");
                    Object.Destroy(m_outputSource.gameObject);
                    m_outputSource = null;
                }
                if (MySyncGroup != null)
                    MySyncGroup.Remove(this);
            }
            return ret;
        }
        /// <summary> Updates curFade, curPlaybackSpeed, curVolume. Call once per audio frame. </summary>
        /// <param name="step">Step size in seconds between frames</param>
        public void UpdateFade(float step) {
            if (CurFade != m_fadeTarget && !(DirectFadeOut && m_isOutputDirect) && PlayToFade == null) {
                if (m_sequence == null || m_sequence.m_trackFadeTime <= 0f)
                    CurFade = m_fadeTarget;
                else
                    CurFade = Mathf.MoveTowards(CurFade, m_fadeTarget, step / m_sequence.m_trackFadeTime);
            }
            if (m_sequence != null) {
                if (!isSynced) { //we are syncronized ... don't adjust playback speed
                    float PlaybackSpeedTarget = m_sequence.PlaybackSpeed;
                    if (curPlaybackSpeed != PlaybackSpeedTarget) {
                        if (m_sequence.m_playbackSpeedFadeTime <= 0f)
                            curPlaybackSpeed = PlaybackSpeedTarget;
                        else
                            curPlaybackSpeed = Mathf.MoveTowards(curPlaybackSpeed, PlaybackSpeedTarget, step / m_sequence.m_playbackSpeedFadeTime);
                    }
                }
                float VolumeTarget = m_sequence.Volume * m_sequence.Clips[currentClipIdx].m_volume;
                if (curVolume != VolumeTarget) {
                    if (m_sequence.m_volumeFadeTime <= 0f)
                        curVolume = VolumeTarget;
                    else
                        curVolume = Mathf.MoveTowards(curVolume, VolumeTarget, step / m_sequence.m_volumeFadeTime);
                }
            }
        }
        /// <summary> Updates clip list if it changed. (Call from within Update() function) </summary>
        public void UpdateClips() {
            if (m_sequence == null)
                return;
            List<AudioClip> allClips = new List<AudioClip>(m_sequence.Clips.Length);
            foreach (Sequence.ClipData clip in m_sequence.Clips) {
                if (clip.m_clip != null)
                    allClips.Add(clip.m_clip);
            }
            if (allClips.Count == 0)
                Debug.LogWarning("No audio clips found in Sequence '" + m_name + "'!");
            bool changed = false;
            if (allClips.Count != DirectClipData.Length) {
                //length changed so it definitly changed.
                changed = true;
            } else {
                for (int c = 0; c < DirectClipData.Length; ++c) {
                    if (DirectClipData[c] != allClips[c]) {
                        changed = true;
                        break;
                    }
                }
            }
            if (changed) { //update current raw clip list then select our current track if it still exists.
                RawAudioData[] newRawClipData = new RawAudioData[allClips.Count];
                AudioClip curClip = m_isOutputDirect ? DirectClipData[currentClipIdx] : RawClipData[currentClipIdx].BaseClip;
                int newClipIdx = -1;
                for (int c = 0; c < allClips.Count; ++c) {
                    if (allClips[c] == curClip)
                        newClipIdx = c;
                    newRawClipData[c] = AmbienceManager.GetAudioData(allClips[c]);
                }
                NextClipIdx = -1;
                if (newClipIdx < 0) {
                    PlayToFade = RawClipData[currentClipIdx];
                    RawClipData = newRawClipData;
                    newClipIdx = NextClipIdx;
                    DirectFadeOut = true;
                } else
                    RawClipData = newRawClipData;
                currentClipIdx = newClipIdx;
                DirectClipData = allClips.ToArray();
                if (m_sequence.RandomizeVolume)
                    curVolumeMod = Helpers.GetRandom(m_sequence.MinMaxVolume);
                else
                    curVolumeMod = 1f;
                if (!isSynced && m_sequence.RandomizePlaybackSpeed)
                    curPlaybackSpeedMod = Helpers.GetRandom(m_sequence.MinMaxPlaybackSpeed);
                else
                    curPlaybackSpeedMod = 1f;
                if (MySyncGroup != null)
                    MySyncGroup.UpdateLength();
            }
        }

        /// <summary> Called by SyncGroup to set this Track's speed and queue based on passed group length </summary>
        /// <param name="newLength">Length of group to match</param>
        internal void UpdateGroupLength(float newLength) {
            if (newLength <= 0 || m_sequence == null) {
                isSynced = false;
                return;
            }
            isSynced = true;
            isDelaying = false;

            //first set up the playback speed ... can we repeat
            curPlaybackSpeed = m_sequence.GetSpeedForSync(newLength);
            curPlaybackSpeedMod = 1f;
            //now set up the queue
            int numTracks = RawClipData.Length;
            List<int> playlist = new List<int>(numTracks);
            for (int i = 0; i < numTracks; ++i)
                playlist.Insert(Helpers.GetRandom(0, i), i);
            trackQueuePos = 0;
            trackQueue = playlist.ToArray();
            if (m_sequence.m_randomizeOrder) //random order
                currentClipIdx = trackQueue[trackQueuePos];
            else //just play sequentially (uses QueuePos as the clip index)
                currentClipIdx = trackQueuePos;

            //now set current track timer (and possibly fast-forward if our group has already started playing)
            trackTime = (AudioSettings.dspTime - MySyncGroup.StartTime) * curPlaybackSpeed;
        }
        #endregion
    }
}