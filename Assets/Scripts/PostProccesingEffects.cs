using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;
using UnityEngine.Rendering.PostProcessing;

public class PostProccesingEffects : MonoBehaviour
{
    public PostProcessVolume volume;

    [Header("Judgment Vignette"), Space(2)]
    public float score;
    public float _intensity;


    private Vignette _Vignette;

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
    }

    // Start is called before the first frame update
    void Start()
    {
        volume.profile.TryGetSettings(out _Vignette);
    }

    // Update is called once per frame
    void Update()
    {
        score = score + _intensity * Time.time;
        _Vignette.intensity.value = score;

    }
}
