// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;

/*
 * Scriptable Object to hold information about a Sequence Modifier
 */

namespace AmbientSounds {
    /// <summary>
    /// Contains Information about how to modify a Sequence
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(menuName = "Procedural Worlds/Ambient Sounds/Modifier")]
    public class Modifier : ScriptableObject, ISerializationCallbackReceiver {
        /// <summary> Are Sliders, Events, or Both required to apply this Modifier? </summary>
        public ValuesOrEvents m_requirements = ValuesOrEvents.None;
        /// <summary> How should Sliders be evaluated? </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("m_slidersMix")]
        public EvaluationType m_valuesMix = EvaluationType.ALL;
        /// <summary> Array of Values to check </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("m_sliders")]
        public SliderRange[] m_values = new SliderRange[0];
        /// <summary> How should Events be evaluated? </summary>
        public EvaluationType m_eventsMix = EvaluationType.ALL;
        /// <summary> Array of Events to check </summary>
        public string[] m_events = new string[0];

        /// <summary> Should Volume be modified while active? </summary>
        public bool m_modVolume = false;
        /// <summary> Value to set Volume to while active </summary>
        [Range(0f, 1f)]
        public float m_volume = 1f;

        /// <summary> Should Playback Speed be modified while active? </summary>
        public bool m_modPlaybackSpeed = false;
        /// <summary> Value to set Playback Speed to while active </summary>
        public float m_playbackSpeed = 1f;

        /// <summary> Should RandomizePlaybackSpeed be modified while active? </summary>
        public bool m_modRandomizePlaybackSpeed = false;
        /// <summary> Value to set RandomizePlaybackSpeed to while active </summary>
        public bool m_randomizePlaybackSpeed = false;
        /// <summary> Should MinMaxPlaybackSpeed be modified while active? </summary>
        public bool m_modMinMaxPlaybackSpeed = false;
        /// <summary> Value to set MinMaxPlaybackSpeed to while active </summary>
        public Vector2 m_minMaxPlaybackSpeed = new Vector2(0f, 1f);

        /// <summary> Should Clips list be modified while active? </summary>
        public bool m_modClips = false;
        /// <summary> How should Clips list be modified? </summary>
        public ClipModType m_modClipsType = ClipModType.Replace;
        /// <summary> Array of Clips to use to while active </summary>
        public Sequence.ClipData[] m_clipData = new Sequence.ClipData[0];
        /// <summary> Legacy clips list (left in to allow deserialization to new structure) </summary>
        [SerializeField]
        AudioClip[] m_clips = new AudioClip[0];
        
        /// <summary> Should DelayChance be modified while active? </summary>
        public bool m_modDelayChance = false;
        /// <summary> Value to set Delay Chance to while active </summary>
        [Range(0f, 100f)]
        public float m_delayChance = 100f;
        
        /// <summary> Should Delay be modified while active? </summary>
        public bool m_modDelay = false;
        /// <summary> Value to set Delay Min/Max to while active </summary>
        public Vector2 m_minMaxDelay = Vector2.zero;

        /// <summary> Should RandomizeVolume be modified while active? </summary>
        public bool m_modRandomizeVolume = false;
        /// <summary> Value to set RandomizeVolume to while active </summary>
        public bool m_randomizeVolume = false;
        /// <summary> Should MinMaxVolume be modified while active? </summary>
        public bool m_modMinMaxVolume = false;
        /// <summary> Value to set MinMaxVolume to while active </summary>
        public Vector2 m_minMaxVolume = new Vector2(0f, 1f);

        /// <summary> Fade value of this Modifier taking Sliders and Events into account </summary>
        public float FadeValue {
            get; set;
        }
        /// <summary> Updates FadeValue. Called when updating events or values. </summary>
        public void UpdateFadeValue() {
            if (m_requirements == ValuesOrEvents.ValuesOrEvents) {
                FadeValue = Mathf.Max(AmbienceManager.CheckEvents(m_events, m_eventsMix) ? 1f : 0f, AmbienceManager.CheckValues(m_values, m_valuesMix));
                return;
            }
            if ((m_requirements & ValuesOrEvents.Events) == ValuesOrEvents.Events)
                if (!AmbienceManager.CheckEvents(m_events, m_eventsMix)) {
                    FadeValue = 0f;
                    return;
                }
            if ((m_requirements & ValuesOrEvents.Values) == ValuesOrEvents.Values)
                FadeValue = AmbienceManager.CheckValues(m_values, m_valuesMix);
            else
                FadeValue = 1f;
        }

        public void OnBeforeSerialize() {
        }

        public void OnAfterDeserialize() {
            if (m_clips != null && m_clips.Length > 0) {
                m_clipData = new Sequence.ClipData[m_clips.Length];
                for (int i = 0; i < m_clips.Length; ++i) {
                    m_clipData[i] = new Sequence.ClipData(m_clips[i], 1f);
                }
                m_clips = null;
            }
        }

    }
}