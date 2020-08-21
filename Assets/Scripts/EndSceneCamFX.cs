using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class EndSceneCamFX : MonoBehaviour
{

    public CinemachineVirtualCamera m_ActiveCamera;

    bool startedEndScene = false;
    float pullBackDuration;
    float currTimer = 0;
    float orginalScreenY;
    [Range(35f,70f)]
    [SerializeField] private float endScreenFOV = 35f;
    [Range(0.5f, 1f)]
    [SerializeField] private float endScreenY = 0.7f;


    private void OnEnable()
    {
        EventSystem.instance.AddListener<EndSceneEvent>(OnEndScene);
    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<EndSceneEvent>(OnEndScene);
    }

    private void Start()
    {
        m_ActiveCamera = gameObject.GetComponent<CinemachineVirtualCamera>();
        orginalScreenY = m_ActiveCamera.GetCinemachineComponent<CinemachineFramingTransposer>().m_ScreenY;
    }

    private void Update()
    {
        EndSceneTriggered();
    }

    void OnEndScene(EndSceneEvent endScene)
    {
        startedEndScene = true;
        pullBackDuration = endScene.m_animationDuration;
    }

    void EndSceneTriggered()
    {
        if (startedEndScene)
        {
            if (pullBackDuration == 0)
            {
                pullBackDuration = 1f;
            }
            if (!m_ActiveCamera.isActiveAndEnabled)
            {
                return;
            }

            if (currTimer < pullBackDuration)
            {
                currTimer += Time.deltaTime;
                m_ActiveCamera.m_Lens.FieldOfView =
                    Mathf.Lerp(20, endScreenFOV, currTimer/pullBackDuration);
                m_ActiveCamera.GetCinemachineComponent<CinemachineFramingTransposer>().m_ScreenY =
                    Mathf.Lerp(orginalScreenY, endScreenY, currTimer / pullBackDuration);
            }
            else
            {

            }
        }
    }

}
