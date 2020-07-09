using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using DualityES;
public class VirtualCameraManager : MonoBehaviour
{
    public CinemachineVirtualCamera PushWorld1Cam;
    public CinemachineVirtualCamera PullWorld2Cam;

    void Awake()
    {
        EventSystem.instance.AddListener<WorldSwitching>(OnWorldSwitch);
        if(PushWorld1Cam == null || PullWorld2Cam == null)
        {
            Debug.LogError("Missing VIrtual Cam Compnents");
        }
    }
    private void OnDisable()
    {
        EventSystem.instance.RemoveListener<WorldSwitching>(OnWorldSwitch);
    }
    void OnWorldSwitch(WorldSwitching world)
    {
        switch (world.targetWorld)
        {
            case Worlds.Pull:
                PullWorld2Cam.enabled = true;
                PushWorld1Cam.enabled = false;
                break;
            case Worlds.Push:
                PushWorld1Cam.enabled = true;
                PullWorld2Cam.enabled = false;
                break;
            default:
                break;
        }
    }

}
