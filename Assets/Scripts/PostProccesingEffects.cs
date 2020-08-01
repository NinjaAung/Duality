using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProccesingEffects : MonoBehaviour
{
    //public PostProcessVolume volume;



    public Volume volume;

    [Header("Judgment Vignette"), Space(2)]
    public float score;
    //public float _intensity;


    [SerializeField]private UnityEngine.Rendering.Universal.Vignette _Vignette;

    public void Awake()
    {
        EventSystem.instance.AddListener<Judgment>(OnJudgment);

    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<Judgment>(OnJudgment);
    }

    void OnJudgment(Judgment judgmentData)
    {
        //Getting realtime Judgment Value
        score = MappingFunction(judgmentData.JudgmentScore, 0, 70, 0, 2);
    }

    // Start is called before the first frame update
    void Start()
    {
        /*
        UnityEngine.Rendering.VolumeProfile volumeProfile = volume?.profile;
        if (!volumeProfile) throw new System.NullReferenceException(nameof(UnityEngine.Rendering.VolumeProfile));


        if (!volumeProfile.TryGet(out _Vignette)) throw new System.NullReferenceException(nameof(_Vignette));
        */
        volume.profile.TryGet(out _Vignette);

    }

    // Update is called once per frame
    void Update()
    {
        //score = score + _intensity * Time.time;

        _Vignette.intensity.Override(score);

    }
    // Found online haven't tested mapping function yet 
    //https://forum.unity.com/threads/re-map-a-number-from-one-range-to-another.119437/
    // Haven't tested yet
    //http://rosettacode.org/wiki/Map_range#C.23
    float MappingFunction(float orignal_value, float aMin, float aMax, float bMin, float bMax)
    {
        float normal = Mathf.InverseLerp(aMin, aMax, orignal_value);
        float bValue = Mathf.Lerp(bMin, bMax, normal);

        return bValue;
    }
}
