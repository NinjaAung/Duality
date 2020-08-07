using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DualityES;

public class CamShake : MonoBehaviour
{

    private CinemachineVirtualCamera m_Camera;
    private CinemachineBasicMultiChannelPerlin noise;
    private float shakeTime;
    private float shakeTimeTotal;
    private float startingIntensity;

    private void OnEnable()
    {
        EventSystem.instance.AddListener<OnNonNativeEvent>(CheckifNonNative);

    }
    void OnDisable()
    {
        EventSystem.instance.AddListener<OnNonNativeEvent>(CheckifNonNative);

    }

    private void Start()
    {

        m_Camera = GetComponent<CinemachineVirtualCamera>();
        noise = m_Camera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    void CheckifNonNative(OnNonNativeEvent contact)
    {
        Shake(1f, 0.5f);
    }

    public void Shake(float intensity, float timer)
    {
        if(noise == null)
        {
            return;
        }
        noise.m_AmplitudeGain = intensity;
        startingIntensity = intensity;
        shakeTime = timer;
        shakeTimeTotal = timer;
    }

    private void Update()
    {
        if(noise == null)
        {
            return;
        }
        if (shakeTime > 0)
        {
            shakeTime -= Time.deltaTime;
            if(shakeTime<= 0)
            {
                //Timer over
                noise.m_AmplitudeGain = Mathf.Lerp(startingIntensity,0f,1- shakeTime/shakeTimeTotal);

            }
        }
    }

}