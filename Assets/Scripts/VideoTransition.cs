using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using DualityES;

public class VideoTransition : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    private bool transtioned = false;

    private void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (videoPlayer == null || videoPlayer.clip == null)
        {
            TranstionOnInvoke();
        }
        else
        {
            if (videoPlayer.isPlaying)
            {
                if (!transtioned)
                {
                    Invoke("TranstionOnInvoke", 6);
                }

            }
        }
    }

    void TranstionOnInvoke()
    {
        EventSystem.instance.RaiseEvent(new SceneLoadNext { });
        //Scenemanager.Instance.NextScene(); Getting an error for no reason :((
        transtioned = true;

    }
}
