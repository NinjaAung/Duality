using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualityES;
using Cinemachine;


public class EndSceneEvent : DualityES.Event
{
    public float m_animationDuration;
}
public class EndPoint: MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EventSystem.instance.RaiseEvent(new EndSceneEvent { m_animationDuration = 10f });//The Visual Effect

        if (CheckpointSystem.finishedPullEndpoint && CheckpointSystem.finishedPushEndpoint)
        {
            EventSystem.instance.RaiseEvent(new EndSceneEvent { m_animationDuration = 3f});//The Visual Effect
            //EventSystem.instance.RaiseEvent(new ResetGameScene { });

        }


        /*
        if (CheckpointSystem.finishedPullEndpoint && CheckpointSystem.finishedPushEndpoint)
        {
            EventSystem.instance.RaiseEvent(new ResetGameScene { });

        }
        */
    }



}
