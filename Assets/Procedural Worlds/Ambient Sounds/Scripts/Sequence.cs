// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;

/*
 * Scriptable Object containing information about a Sequence
 */

namespace AmbientSounds {
    /// <summary> Contains information about a Sequence of clips to play and how </summary>
    [CreateAssetMenu(menuName = "Procedural Worlds/Ambient Sounds/Sequence")]
    public class Sequence : ScriptableObject, ISerializationCallbackReceiver {
        #region Types
        [System.Serializable]
        public class PlayEvent : UnityEngine.Events.UnityEvent<AudioClip> {
            public PlayEvent() { }
        }
        [System.Serializable]
        public struct ClipData {
            public AudioClip m_clip;
            public float m_volume;

            public ClipData(AudioClip Clip, float Volume) {
                m_clip = Clip;
                m_volume = Volume;
            }

            public static implicit operator ClipData(AudioClip clip) {
                return new ClipData(clip, 1f);
            }
        }
        #endregion
        #region Public Variables
        /// <summary> Mixture of Values or Events required to play </summary>
        public ValuesOrEvents m_requirements = ValuesOrEvents.None;
        /// <summary> Mixture of Values required for Sequence to play </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("m_slidersMix")]
        public EvaluationType m_valuesMix = EvaluationType.ALL;
        /// <summary> Value values required for Sequence to play </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("m_sliders")]
        public SliderRange[] m_values = new SliderRange[0];
        /// <summary> Mixture of events required for Sequence to play </summary>
        public EvaluationType m_eventsMix = EvaluationType.ALL;
        /// <summary> Events required for Sequence to play </summary>
        public string[] m_events = new string[0];

        /// <summary> Audio Clips to play </summary>
        public ClipData[] m_clipData = new ClipData[0];
        /// <summary> Legacy clips list (left in to allow deserialization to new structure) </summary>
        [SerializeField]
        [System.Obsolete("m_clips is no longer used. Use m_clipData instead.")]
        AudioClip[] m_clips = new AudioClip[0];
        /// <summary> Volume this Sequence plays at </summary>
        [Range(0,1)]
        public float m_volume = 1f;
        /// <summary> Rate at which to play audio clips. 1.0 = normal, 2.0 = double </summary>
        public float m_playbackSpeed = 1f;
        /// <summary> Should clips play sequentially or randomly? </summary>
        public bool m_randomizeOrder = false;
        /// <summary> Time in seconds to cross-fade between clips </summary>
        public float m_crossFade = 0f;
        /// <summary> Time in seconds to fade track in/out when starting/stopping </summary>
        public float m_trackFadeTime = 2.5f;
        /// <summary> Time in seconds to fade volume when changed </summary>
        public float m_volumeFadeTime = 1f;
        /// <summary> Time in seconds to fade playback speed when changed </summary>
        public float m_playbackSpeedFadeTime = 1f;

        /// <summary> Percent chance to delay playback between tracks </summary>
        [Range(0f, 100f)]
        public float m_delayChance = 100f;
        /// <summary> Time in seconds to fade track out for delay </summary>
        public float m_delayFadeTime = 0f;
        /// <summary> Minimum time in seconds between playback of this Sequence </summary>
        public Vector2 m_minMaxDelay = Vector2.zero;

        /// <summary> Should Volume be Randomized? </summary>
        public bool m_randomVolume = false;
        /// <summary> Min/Max values for Randomized Volume </summary>
        public Vector2 m_minMaxVolume = new Vector2(0f, 1f);
        /// <summary> Should PlaybackSpeed be Randomized? </summary>
        public bool m_randomPlaybackSpeed = false;
        /// <summary> Min/Max values for Randomized PlaybackSpeed </summary>
        public Vector2 m_minMaxPlaybackSpeed = new Vector2(0f, 1f);
        
        /// <summary> List of modifiers that can be applied to this Sequence </summary>
        public Modifier[] m_modifiers = new Modifier[0];

        /// <summary> Output type to use for this Sequence </summary>
        public OutputType m_outputType = OutputType.STRAIGHT;
        /// <summary> Prefab to spawn for AudioSource output. (null will create default empty object) </summary>
        public GameObject m_outputPrefab = null;
        /// <summary> Should output always use AudioSource and bypass effects like cross-fade? </summary>
        public bool m_outputDirect = false;
        /// <summary> Min/Max distance to place AudioSource when output type is not "STRAIGHT" </summary>
        public Vector2 m_outputDistance = Vector2.zero;
        /// <summary> Min/Max angle (in degrees) to place AudioSource when output type is not "STRAIGHT" </summary>
        public Vector2 m_outputVerticalAngle = new Vector2(-180, 180);
        /// <summary> Min/Max angle (in degrees) to place AudioSource when output type is not "STRAIGHT" </summary>
        public Vector2 m_outputHorizontalAngle = new Vector2(-180, 180);
        /// <summary> Does the output move with the Camera/Player/Area? </summary>
        public bool m_outputFollowPosition = true;
        /// <summary> Does the output rotate with the Camera/Player/Area? </summary>
        public bool m_outputFollowRotation = false;

        /// <summary> Name of SyncGroup to follow </summary>
        public string m_syncGroup = "";
        /// <summary> How to syncronize with the SyncGroup </summary>
        public SyncType m_syncType = SyncType.REPEAT;

        /// <summary> List of events to trigger while playing this Sequence </summary>
        public string[] m_eventsWhilePlaying = new string[0];
        /// <summary> List of values to update while playing this Sequence (will set to max value of all Sequences with this value) </summary>
        public string[] m_valuesWhilePlaying = new string[0];

        /// <summary> Event fired off when a Clip is starting to play (when it is first read into the audio buffer) </summary>
        public PlayEvent m_OnPlayClip = new PlayEvent();
        /// <summary> Event fired off when a Clip stops playing (when it is no longer being read into the audio buffer) </summary>
        public PlayEvent m_OnStopClip = new PlayEvent();
        #endregion
        #region Internal Use Variables
        /// <summary> Used by editor to show fade values during play. </summary>
        [System.NonSerialized]
        internal float m_lastFade = 0f;
        /// <summary> Used to indicate this Sequence needs to have UpdateModifiers() ran </summary>
        [System.NonSerialized]
        internal bool m_needsUpdateModifiers = true;
        /// <summary> Used by Editor to mute Sequences for debug </summary>
        [System.NonSerialized]
        public bool m_forceMuted = false;
        /// <summary> Used by Editor to force playback, ignoring Requirements </summary>
        [System.NonSerialized]
        public bool m_forcePlay = false;
        /// <summary> Used by Editor to check UpdateModifiers() has been called at least once </summary>
        [System.NonSerialized]
        internal bool m_hasBeenUpdated = false;
        #endregion
        #region Serialization

        public void OnBeforeSerialize() {
        }

        public void OnAfterDeserialize() {
#pragma warning disable
            if (m_clips != null && m_clips.Length > 0) {
                m_clipData = new ClipData[m_clips.Length];
                for (int i = 0; i < m_clips.Length; ++i) {
                    m_clipData[i] = new ClipData(m_clips[i], 1f);
                }
                m_clips = null;
            }
#pragma warning restore
        }
        #endregion
        #region Properties
        /// <summary> Internal value for time in seconds to wait between tracks </summary>
        private float _trackDelayTime = -1f;
        /// <summary> Helper to automatically select delay time based on Modifiers and chance and min/max values </summary>
        public float TrackDelayTime {
            get {
                if (_trackDelayTime < 0f) {
                    float delayChance = m_delayChance;
                    Vector2 minMaxDelay = m_minMaxDelay;
                    for(int m = 0; m < m_modifiers.Length; ++m) {
                        if (m_modifiers[m] == null)
                            continue;
                        if (m_modifiers[m].m_modDelayChance) {
                            if (m_modifiers[m].FadeValue <= 0f)
                                continue;
                            if (m_modifiers[m].FadeValue >= 1f)
                                delayChance = m_modifiers[m].m_delayChance;
                            else
                                delayChance = m_modifiers[m].m_delayChance * m_modifiers[m].FadeValue + delayChance * (1f - m_modifiers[m].FadeValue);
                        }
                        if (m_modifiers[m].m_modDelay) {
                            float modVal = m_modifiers[m].FadeValue;
                            if (modVal <= 0f)
                                continue;
                            if (modVal >= 1f)
                                minMaxDelay = m_modifiers[m].m_minMaxDelay;
                            else
                                minMaxDelay = m_modifiers[m].m_minMaxDelay * modVal + minMaxDelay * (1f - modVal);
                        }
                    }
                    if (delayChance > 0f && (delayChance >= 100f || Helpers.GetRandom(0f, 100f) <= delayChance))
                        _trackDelayTime = Helpers.GetRandom(minMaxDelay.x, minMaxDelay.y);
                    else
                        _trackDelayTime = 0f;
                }
                return _trackDelayTime;
            }
            set {
                _trackDelayTime = value;
            }
        }
        /// <summary> Helper to get fade values given current Values and Events </summary>
        public float FadeValue {
            get {
                if (m_requirements == ValuesOrEvents.ValuesOrEvents)
                    return m_lastFade = Mathf.Max(AmbienceManager.CheckEvents(m_events, m_eventsMix) ? 1f : 0f, AmbienceManager.CheckValues(m_values, m_valuesMix));
                if ((m_requirements & ValuesOrEvents.Events) == ValuesOrEvents.Events)
                    if (!AmbienceManager.CheckEvents(m_events, m_eventsMix))
                        return m_lastFade = 0f;
                if ((m_requirements & ValuesOrEvents.Values) == ValuesOrEvents.Values)
                    return m_lastFade = AmbienceManager.CheckValues(m_values, m_valuesMix);
                else
                    return m_lastFade = 1f;
            }
        }
        
        /// <summary> Total playbackSpeed taking all modifiers into account </summary>
        public float PlaybackSpeed {
            get {
                float ret = m_playbackSpeed;
                for (int m = 0; m < m_modifiers.Length; ++m)
                    if (m_modifiers[m] != null && m_modifiers[m].m_modPlaybackSpeed)
                        ret = ret * (1f - m_modifiers[m].FadeValue) + m_modifiers[m].m_playbackSpeed * m_modifiers[m].FadeValue;
                return ret;
            }
        }

        /// <summary> Randomize PlaybackSpeed option based on current modifiers </summary>
        public bool RandomizePlaybackSpeed {
            get {
                if (_randomizePlaybackSpeedModIsSet)
                    return _randomizePlaybackSpeedMod;
                return m_randomPlaybackSpeed;
            }
        }
        /// <summary> Min/Max Random Volume based on current modifiers </summary>
        public Vector2 MinMaxPlaybackSpeed {
            get {
                Vector2 ret = m_minMaxPlaybackSpeed;
                for (int m = 0; m < m_modifiers.Length; ++m)
                    if (m_modifiers[m] != null && m_modifiers[m].m_modMinMaxPlaybackSpeed)
                        ret = ret * (1f - m_modifiers[m].FadeValue) + m_modifiers[m].m_minMaxPlaybackSpeed * m_modifiers[m].FadeValue;
                return ret;
            }
        }

        /// <summary> Total volume taking all modifiers into account </summary>
        public float Volume {
            get {
                float ret = m_volume;
                for (int m = 0; m < m_modifiers.Length; ++m)
                    if (m_modifiers[m] != null && m_modifiers[m].m_modVolume)
                        ret = ret * (1f - m_modifiers[m].FadeValue) + m_modifiers[m].m_volume * m_modifiers[m].FadeValue;
                return ret;
            }
        }
        /// <summary> Clips list based on Modifiers </summary>
        public ClipData[] Clips {
            get; private set;
        }
        internal object ClipsLockHandle = new object();
        /// <summary> Randomize Volume option based on current modifiers </summary>
        public bool RandomizeVolume {
            get {
                if (_randomizeVolumeModIsSet)
                    return _randomizeVolumeMod;
                return m_randomVolume;
            }
        }
        /// <summary> Min/Max Random Volume based on current modifiers </summary>
        public Vector2 MinMaxVolume {
            get {
                Vector2 ret = m_minMaxVolume;
                for (int m = 0; m < m_modifiers.Length; ++m)
                    if (m_modifiers[m] != null && m_modifiers[m].m_modMinMaxVolume)
                        ret = ret * (1f - m_modifiers[m].FadeValue) + m_modifiers[m].m_minMaxVolume * m_modifiers[m].FadeValue;
                return ret;
            }
        }
        
        /// <summary> Total length of all clips in this Sequence </summary>
        public float TotalLength {
            get; private set;
        }
        #endregion
        #region Public Functions
        bool _randomizeVolumeModIsSet = false;
        bool _randomizeVolumeMod = false;
        bool _randomizePlaybackSpeedModIsSet = false;
        bool _randomizePlaybackSpeedMod = false;
        /// <summary> To be called every time a Modifier changes so the properties can be recalculated </summary>
        public void UpdateModifiers() {
            m_hasBeenUpdated = true;
            List<ClipData> clips = new List<ClipData>(m_clipData);
            for(int m = 0; m < m_modifiers.Length; ++m) {
                if (m_modifiers[m] == null)
                    continue;
                m_modifiers[m].UpdateFadeValue();
                if (m_modifiers[m].m_modRandomizeVolume) {
                    if (m_modifiers[m].FadeValue > 0.5f) {
                        _randomizeVolumeModIsSet = true;
                        _randomizeVolumeMod = m_modifiers[m].m_randomizeVolume;
                    }
                }
                if (m_modifiers[m].m_modRandomizePlaybackSpeed) {
                    if (m_modifiers[m].FadeValue > 0.5f) {
                        _randomizePlaybackSpeedModIsSet = true;
                        _randomizePlaybackSpeedMod = m_modifiers[m].m_randomizePlaybackSpeed;
                    }
                }
                if (m_modifiers[m].m_modClips) {
                    if (m_modifiers[m].FadeValue >= 0.5f) { //can only change the array fully or not at all ... so do it at half way up
                        if (m_modifiers[m].m_modClipsType == ClipModType.Add) {
                            clips.AddRange(m_modifiers[m].m_clipData);
                        } else if (m_modifiers[m].m_modClipsType == ClipModType.Remove) {
                            foreach (ClipData clip in m_modifiers[m].m_clipData) {
                                if (clip.m_clip != null)
                                    clips.RemoveAll(delegate (ClipData c) {
                                        return c.m_clip == clip.m_clip;
                                    });
                            }
                        } else { //Replace
                            clips = new List<ClipData>(m_modifiers[m].m_clipData);
                        }
                    }
                }
            }
            float newLength = 0f;
            clips.RemoveAll(c => c.m_clip == null);
            foreach (ClipData clip in clips) {
                newLength += (AmbienceManager.GetAudioData(clip.m_clip).Length - m_crossFade) * PlaybackSpeed;
                //newLength += (clip.length - m_crossFade) * PlaybackSpeed;
            }
            if (newLength != TotalLength) {
                TotalLength = newLength;
                if (!string.IsNullOrEmpty(m_syncGroup))
                    SyncGroup.Get(m_syncGroup).UpdateLength();
            }
            lock(ClipsLockHandle)
                Clips = clips.ToArray();
        }
        
        /// <summary> Gets PlaybackSpeed for this Sequence with given Timeframe for SyncGroup </summary>
        /// <param name="timeframe">Total time to fit this track within</param>
        /// <returns></returns>
        public float GetSpeedForSync(float timeframe) {
            float myLength = TotalLength;
            SyncType sType = m_syncType;
            if ((sType & SyncType.REPEAT) > 0) { //if we can repeat then we need to find out how many complete sets will fit into the length
                float totalLength = Mathf.Floor(timeframe / myLength) * myLength;
                if ((sType & SyncType.FIT) == SyncType.FIT) { //we can both stretch and squeeze
                    if (totalLength < myLength || (timeframe - totalLength) / myLength < 0.5f)
                        totalLength += myLength;
                    return totalLength / timeframe;
                } else if ((sType & SyncType.SQUEEZE) > 0) //we can only squeeze so we need to increase a repeat and squeeze down
                    return (totalLength == timeframe ? 1f : (totalLength + myLength) / timeframe);
                else if ((sType & SyncType.STRETCH) > 0) //we can only stretch so we will just need to stretch the current count
                    return totalLength / timeframe;
                else
                    return PlaybackSpeed;
            } else { //if we can't repeat then just try and fit as best we can (or we will just play once and wait for the next set)
                if ((timeframe > myLength && (sType & SyncType.STRETCH) > 0) || (timeframe < myLength && (sType & SyncType.SQUEEZE) > 0))
                    return myLength / timeframe;
                else //can't fit so just play as-is (or Length matches up perfect)
                    return PlaybackSpeed;
            }
        }
        #endregion
        #region Context Menu Functions
        [ContextMenu("Play", true)]
        bool CanAddPlayMenuItem() {
            return Application.isPlaying && !AmbienceManager.WasSequenceAdded(this);
        }
        [ContextMenu("Play")]
        void PlayMenuItem() {
            m_forcePlay = true;
            AmbienceManager.AddSequence(this);
        }
        [ContextMenu("Stop", true)]
        bool CanAddStopMenuItem() {
            return Application.isPlaying && AmbienceManager.WasSequenceAdded(this);
        }
        [ContextMenu("Stop")]
        void StopMenuItem() {
            m_forcePlay = false;
            AmbienceManager.RemoveSequence(this);
        }
        #endregion
        private void Awake() {
            m_needsUpdateModifiers = true;
            m_forceMuted = false;
            m_forcePlay = false;

            
        }
    }
}
