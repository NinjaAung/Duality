// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;

/// <summary> Example of how to trigger Ambient Sounds' Play() function for AudioClips </summary>
public class PlayOnceExample : MonoBehaviour {
    /// <summary> AudioClip to play </summary>
    [Tooltip("AudioClip to play")]
    public AudioClip toPlay = null;

    /// <summary> Call to play clip once </summary>
    public void PlayClip() {
        AmbientSounds.AmbienceManager.Play(toPlay, 1f);
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(PlayOnceExample))]
public class PlayOnceExampleEditor : UnityEditor.Editor {
    public override void OnInspectorGUI() {
        UnityEditor.EditorGUILayout.HelpBox("Example of how to trigger Ambient Sounds' Play function clips\nClick 'Play Clip' button while editor is playing", UnityEditor.MessageType.Info, true);
        base.OnInspectorGUI();
        // Add button to inspector while playing to trigger from editor
        if (Application.isPlaying && GUILayout.Button("Play Clip")) {
            ((PlayOnceExample)target).PlayClip();
        }
    }
}
#endif
