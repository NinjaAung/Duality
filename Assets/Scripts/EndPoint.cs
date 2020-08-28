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
    private bool m_AtEnd = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        EventSystem.instance.RaiseEvent(new EndSceneEvent { m_animationDuration = 10f });//The Visual Effect
        Invoke("FinishedEndScene",10f);
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

    private void FinishedEndScene()
    {
        m_AtEnd = true;
        Debug.Log("In the function");
    }


    private void Update()
    {
        if (m_AtEnd)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("In the update function");

                EventSystem.instance.RaiseEvent(new ResetGameScene { });
                m_AtEnd = false;

            }
        }
    }


}
