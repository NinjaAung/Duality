// Copyright © 2018 Procedural Worlds Pty Limited.  All Rights Reserved.
using UnityEngine;
using System.Collections;

/// <summary> Example of how to add or remove a Sequence from AmbienceManager through script interface </summary>
public class AddRemoveSequenceExample : MonoBehaviour {
    /// <summary> Sequence to add/remove </summary>
    [Tooltip("Sequence to add/remove")]
    public AmbientSounds.Sequence SequenceToAdd = null;

    /// <summary> Adds Sequence to AmbienceManager </summary>
    public void AddSequence() {
        AmbientSounds.AmbienceManager.AddSequence(SequenceToAdd);
    }
    /// <summary> Removes Sequence from AmbienceManager </summary>
    public void RemoveSequence() {
        AmbientSounds.AmbienceManager.RemoveSequence(SequenceToAdd);
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(AddRemoveSequenceExample))]
public class AddRemoveSequenceExampleEditor : UnityEditor.Editor {
    public override void OnInspectorGUI() {
        UnityEditor.EditorGUILayout.HelpBox("Example of how to add or remove a Sequence from AmbienceManager through script interface\nClick 'Add' or 'Remove' buttons while editor is playing", UnityEditor.MessageType.Info, true);
        base.OnInspectorGUI();
        // Add buttons to inspector in editor while playing
        if (Application.isPlaying) {
            if (GUILayout.Button("Add")) {
                ((AddRemoveSequenceExample)target).AddSequence();
            }
            if (GUILayout.Button("Remove")) {
                ((AddRemoveSequenceExample)target).RemoveSequence();
            }
        }
    }
}
#endif
