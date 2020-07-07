using UnityEngine;

/// <summary> Example of how to use a Ambient Sounds 'Slider' to track a value such as "Time of Day" </summary>
public class SliderExample : MonoBehaviour {
    /// <summary> Name of Slider to update </summary>
    [Tooltip("Name of Slider to update")]
    public string SliderToSet = "Time of Day";
    /// <summary> Time in seconds to make a full revolution from 0 to 1 </summary>
    [Tooltip("Time in seconds to make a full revolution from 0 to 1")]
    public float SliderDuration = 120.0f;
    /// <summary> Value to begin Slider at </summary>
    [Tooltip("Value to begin Slider at")]
    public float SliderStartVal = 0.2f;

    /// <summary> Current value of Slider </summary>
    [Tooltip("Current value of Slider")]
    float curSliderVal = 0f;

    private void OnEnable() {
        curSliderVal = SliderStartVal;
        //start off by setting base value
        AmbientSounds.AmbienceManager.SetValue(SliderToSet, curSliderVal);
    }
    void Update () {
        //check for invalid duration (prevent DivByZero errors)
        if (SliderDuration <= 0f)
            return;
        //add to current value
        curSliderVal += Time.deltaTime / SliderDuration;
        //loop value within range [0-1]
        if (curSliderVal > 1f)
            curSliderVal -= 1f;
        if (curSliderVal < 0f)
            curSliderVal += 1f;
        //Set actual value in AmbienceManager
        AmbientSounds.AmbienceManager.SetValue(SliderToSet, curSliderVal);
    }
    private void OnDisable() {
        //Remove slider when this script is disabled
        AmbientSounds.AmbienceManager.RemoveValue(SliderToSet);
    }
    private void OnDestroy() {
        //Remove slider when this script is destroyed
        AmbientSounds.AmbienceManager.RemoveValue(SliderToSet);
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(SliderExample))]
public class SliderExampleEditor : UnityEditor.Editor {
    public override void OnInspectorGUI() {
        UnityEditor.EditorGUILayout.HelpBox("Example of how to use a Ambient Sounds 'Slider' to track a value such as \"Time of Day\"\nWill update automatically while script is active.", UnityEditor.MessageType.Info, true);
        base.OnInspectorGUI();
    }
}
#endif
