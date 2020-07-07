// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections.Generic;

namespace AmbientSounds {
    internal class SyncGroup {
        public string m_name = "";

        List<AudioTrack> tracks = new List<AudioTrack>();
        double lastStartTime = 0f;
        double lastEndTime = 0f;

        public double StartTime {
            get {
                if (lastStartTime == 0f || AudioSettings.dspTime >= lastEndTime)
                    UpdateStartTime();
                return lastStartTime;
            }
        }
        public double EndTime {
            get {
                if (lastStartTime == 0f || AudioSettings.dspTime >= lastEndTime)
                    UpdateStartTime();
                return lastEndTime;
            }
        }

        public SyncGroup(string name) {
            m_name = name;
        }

        public void Add(AudioTrack track) {
            if (track == null)
                return;
            if (!tracks.Contains(track)) {
                Debug.Log("Adding Track " + track.m_name + " to SyncGroup " + m_name);
                tracks.Add(track);
                UpdateLength();
            }
        }
        public void Remove(AudioTrack track) {
            if (track == null)
                return;
            if (tracks.Contains(track)) {
                Debug.Log("Removing Track " + track.m_name + " from SyncGroup " + m_name);
                tracks.Remove(track);
                UpdateLength();
            }
        }

        public float UpdateLength() {
            float maxLength = 0f;
            float minLength = -1;
            float maxFlexLength = 0f;
            for (int t = 0; t < tracks.Count; ++t) {
                if (tracks[t] == null || tracks[t].m_sequence == null || tracks[t].m_sequence.m_syncGroup != m_name) {
                    tracks.RemoveAt(t--);
                    continue;
                }
                float length = tracks[t].m_sequence.TotalLength;
                SyncType sType = tracks[t].m_sequence.m_syncType;
                if ((sType & SyncType.STRETCH) == 0 && (minLength > length || minLength < 0))
                    minLength = length;
                if ((sType & SyncType.SQUEEZE) > 0) {
                    if (maxFlexLength < length)
                        maxFlexLength = length;
                } else if (maxLength < length)
                    maxLength = length;
            }
            if (tracks.Count == 0) { //we have no tracks left so nothing to update
                lastEndTime = 0;
                return 0f;
            }
            float newLength = Mathf.Max(minLength, maxLength == 0 ? maxFlexLength : maxLength);
            lastEndTime = lastStartTime + newLength;
            foreach (AudioTrack track in tracks)
                track.UpdateGroupLength(newLength);
            return newLength;
        }
        public void UpdateStartTime() {
            lastStartTime = AudioSettings.dspTime;
            UpdateLength();
        }
        #region Static References
        static List<SyncGroup> _allSyncGroups = new List<SyncGroup>();
        internal static SyncGroup Get(string groupName) {
            if (string.IsNullOrEmpty(groupName))
                return null;
            SyncGroup ret = _allSyncGroups.Find(delegate (SyncGroup group) {
                return groupName == group.m_name;
            });
            if (ret == null && !string.IsNullOrEmpty(groupName)) {
                ret = new SyncGroup(groupName);
                _allSyncGroups.Add(ret);
            }
            return ret;
        }
        public static void UpdateAll() {
            double dspTime = AudioSettings.dspTime;
            string allGroupNames = "";
            foreach (SyncGroup sg in _allSyncGroups) {
                allGroupNames += sg.m_name + ", ";
                if (sg.lastStartTime == 0f || dspTime >= sg.lastEndTime)
                    sg.UpdateStartTime();
            }
            _allSyncGroups.RemoveAll(delegate (SyncGroup sg) { return sg.tracks.Count == 0; });
        }
        #endregion
    }
}